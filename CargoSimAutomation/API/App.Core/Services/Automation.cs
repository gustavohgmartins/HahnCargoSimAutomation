using App.Core.Clients;
using App.Core.Hubs;
using App.Domain.DTOs;
using App.Domain.Model;
using App.Domain.Models;
using App.Domain.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace App.Core.Services
{
    public class Automation : IAutomation
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        private readonly AuthDto authUser;
        private readonly AutomationHub hub;
        private readonly IConfiguration configuration;
        private string _token;
        private bool _isRunning;
        private int _maxTransporters;
        private int _coins;
        private Grid _grid;
        private Graph _graph;
        private List<Order> _availableOrders = new List<Order>();
        private List<Order> _acceptedOrders = new List<Order>();
        private List<CargoTransporter> _transporters = new List<CargoTransporter>();
        private List<int> _transportersIds = new List<int>();
        private Dictionary<int, Order> _transporterAcceptedOrder = new Dictionary<int, Order>();
        private Dictionary<string,List<string>> _logs = new Dictionary<string, List<string>>();


        public Automation(HahnCargoSimClient hahnCargoSimClient, AuthDto authUser, IConfiguration configuration, AutomationHub hub)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
            this.authUser = authUser;
            this.configuration = configuration;
            this.hub = hub;
            _maxTransporters = configuration.GetValue<int>("MaxTransporters");
        }

        public async Task Start(string token)
        {
            _token = token;

            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            Task.Run(ExecuteAsync);
        }

        public async Task Stop()
        {
            _isRunning = false;
        }

        private async Task ExecuteAsync()
        {
            _grid = await hahnCargoSimClient.GetGrid(_token);
            _graph = new Graph(_grid.Nodes, _grid.Edges, _grid.Connections);

            await hub.SendLog(authUser.Username, "Simulation", $"Hello, {authUser.Username}!");
            await hub.SendLog(authUser.Username, "Simulation", "Simulation started");

            while (_isRunning)
            {
                _coins = await hahnCargoSimClient.GetCoinAmount(_token);

                await GetOrders();

                if (!_availableOrders.Any() && !_acceptedOrders.Any())
                {
                    await hub.SendLog(authUser.Username, "Simulation", "Waiting for available orders...");

                    while (!_availableOrders.Any())
                    {
                        await GetOrders();
                        await Task.Delay(1000);
                    }
                }

                await GetTransporters();
                await BuyTransporter();
                await MoveTransporters();

                await Task.Delay(2000);
            }


            await hub.SendLog(authUser.Username, "Simulation", "Finished");

        }

        private async Task GetTransporters()
        {
            _transporters = [];

            if (_transportersIds.Any())
            {
                foreach (var id in _transportersIds)
                {
                    var transporter = await hahnCargoSimClient.GetCargoTransporter(_token, id);
                    if (transporter is not null)
                    {
                        if (_transporterAcceptedOrder.ContainsKey(transporter.Id))
                        {
                            var acceptedOrder = _transporterAcceptedOrder[transporter.Id];

                            if (transporter.LoadedOrders.Contains(acceptedOrder))
                            {
                                _transporterAcceptedOrder.Remove(transporter.Id);
                            }
                            else
                            {
                                transporter.AcceptedOrder = acceptedOrder;
                            }
                        }

                        transporter.Route = [];

                        if (!transporter.InTransit)
                        {
                            transporter = await BuildRoute(transporter);
                        }

                        _transporters.Add(transporter);
                    }
                }
            }
        }

        private async Task<CargoTransporter> BuildRoute(CargoTransporter transporter)
        {
            if (transporter.LoadedOrders.Any())
            {
                var bestOrder = transporter.LoadedOrders
                    .Select(o => new
                    {
                        Route = _graph.GetRoute(transporter.PositionNodeId, o.TargetNodeId),
                        Order = o
                    })
                    .OrderBy(r => r.Route.Time)
                    .First();

                transporter.Route = bestOrder.Route.Route;

                await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Route updated. Current route: {string.Join(" -> ", transporter.Route.Select(r => GetNodeName(r)).ToList())} | Currently on route to delivery order {bestOrder.Order.Id} | Estimated time for delivery: {bestOrder.Route.Time}");
            }
            else if (transporter.AcceptedOrder is not null)
            {
                var route = _graph.GetRoute(transporter.PositionNodeId, transporter.AcceptedOrder.OriginNodeId);

                transporter.Route = route.Route;

                if (route.Time != TimeSpan.Zero)
                {
                    await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Route updated. Current route: {string.Join(" -> ", transporter.Route.Select(r => GetNodeName(r)).ToList())} | Currently on route to pick up order {transporter.AcceptedOrder.Id} | Estimated time for pick up: {route.Time}");
                }
            }
            ////Accepts the next order and sets the transporter on route to pick it up
            else
            {
                var bestOrder = _availableOrders
                    .Select(o => new
                    {
                        OrderRoute = _graph.GetRoute(o.OriginNodeId, o.TargetNodeId),
                        PickUpRoute = _graph.GetRoute(transporter.PositionNodeId, o.OriginNodeId),
                        Order = o
                    })
                    .OrderByDescending(r => BestRouteIndex(r.OrderRoute.Time + r.PickUpRoute.Time, r.OrderRoute.Cost + r.PickUpRoute.Cost, r.Order.Value))
                    .First();

                var orderAccepted = await hahnCargoSimClient.AcceptOrder(_token, bestOrder.Order.Id);

                if (orderAccepted)
                {
                    await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Order {bestOrder.Order.Id} accepted from {GetNodeName(bestOrder.Order.OriginNodeId)} to {GetNodeName(bestOrder.Order.TargetNodeId)}");

                    ////Set the accepted order to the current transporter
                    _transporterAcceptedOrder[transporter.Id] = bestOrder.Order;

                    transporter.AcceptedOrder = bestOrder.Order;

                    transporter.Route = bestOrder.PickUpRoute.Route;

                    if (bestOrder.PickUpRoute.Time != TimeSpan.Zero)
                    {
                        await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Route updated. Current route: {string.Join(" -> ", transporter.Route.Select(r => GetNodeName(r)).ToList())} | Currently on route to pick up order {bestOrder.Order.Id}  Estimated time for pick up: {bestOrder.PickUpRoute.Time}");
                    }
                    else
                    {
                        await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Picking up order {bestOrder.Order.Id}");
                    }
                }
            }

            return transporter;
        }

        private async Task BuyTransporter()
        {
            if (_maxTransporters > 0 && _transporters.Count() >= _maxTransporters)
            {
                return;
            }

            if (!_transportersIds.Any() || _coins > (1000 * (1 + 0.1 * _transporters.Count)))

            ////if (!_transportersIds.Any())
            {
                var bestOrder = _availableOrders
                    .Select(o => new
                    {
                        OrderRoute = _graph.GetRoute(o.OriginNodeId, o.TargetNodeId),
                        Order = o
                    })
                    .OrderByDescending(r => BestRouteIndex(r.OrderRoute.Time, r.OrderRoute.Cost, r.Order.Value))
                    .First();

                var orderAccepted = await hahnCargoSimClient.AcceptOrder(_token, bestOrder.Order.Id);

                if (orderAccepted)
                {
                    var transporterId = await hahnCargoSimClient.BuyCargoTransporter(_token, bestOrder.Order.OriginNodeId);


                    _transportersIds.Add(transporterId);

                    _transporterAcceptedOrder[transporterId] = bestOrder.Order;

                    await hub.SendLog(authUser.Username, $"Simulation", $"Transporter {transporterId} bought at {GetNodeName(bestOrder.Order.OriginNodeId)}");
                    await hub.SendLog(authUser.Username, $"Transporter {transporterId}", $"Order {bestOrder.Order.Id} accepted from {GetNodeName(bestOrder.Order.OriginNodeId)} to {GetNodeName(bestOrder.Order.TargetNodeId)}. Estimated delivery time {bestOrder.OrderRoute.Time}");
                    await hub.SendLog(authUser.Username, $"Transporter {transporterId}", $"Loading order {bestOrder.Order.Id}");
                }
            }
        }

        private async Task MoveTransporters()
        {
            var transportersToMove = _transporters.Where(t => t.Route.Any() && !t.InTransit);//Filters the transporters that have a route and are not already in transit

            foreach (var transporter in transportersToMove)
            {
                var targetNode = transporter.Route[0];

                var moveTransporter = await hahnCargoSimClient.MoveCargoTransporter(_token, transporter.Id, targetNode);

                if (moveTransporter)
                {
                    await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Moving from {GetNodeName(transporter.PositionNodeId)} to {GetNodeName(targetNode)}");

                    transporter.Route.RemoveAt(0);

                    await AcceptNodeOrders(transporter, targetNode);//Accepts any acceptable order in the target node, so it can load it as he gets there

                    if (!transporter.Route.Any())
                    {
                        _transporterAcceptedOrder.Remove(transporter.Id);
                        if (transporter.LoadedOrders.Any(o => o.TargetNodeId == targetNode))
                        {
                            await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Delivering the following orders: {string.Join(", ", transporter.LoadedOrders.Where(o => o.TargetNodeId == targetNode).Select(o => o.Id).ToList())}");
                        }
                        else
                        {
                            await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Picking up order {transporter.AcceptedOrder.Id}");
                        }
                    }
                }
            }
        }
        private bool OrderAboutToExpire(Order? order)
        {
            if (order == null)
            {
                return false;
            }
            var deliveryDateTime = DateTime.ParseExact(order.DeliveryDateUtc, "MM/dd/yyyy HH:mm:ss", null);

            return deliveryDateTime <= DateTime.UtcNow;
        }
        private async Task AcceptNodeOrders(CargoTransporter transporter, int nodeId)
        {
            if (transporter.LoadedOrders.Any(o => OrderAboutToExpire(o)) || OrderAboutToExpire(transporter.AcceptedOrder))
            {
                return;
            }

            List<Order> acceptedOrders = new List<Order>();

            var nodeAvailableOrders = _availableOrders
                    .Where(o => o.OriginNodeId == nodeId)
                    .Select(o => new
                    {
                        OrderRoute = _graph.GetRoute(o.OriginNodeId, o.TargetNodeId),
                        Order = o
                    })
                    .OrderByDescending(r => BestRouteIndex(r.OrderRoute.Time, r.OrderRoute.Cost, r.Order.Value));

            var currentLoad = transporter.Load + (transporter.AcceptedOrder?.Load ?? 0);

            foreach (var order in nodeAvailableOrders)
            {
                if (currentLoad + order.Order.Load <= transporter.Capacity)
                {
                    var orderAccepted = await hahnCargoSimClient.AcceptOrder(_token, order.Order.Id);

                    if (orderAccepted)
                    {
                        currentLoad += order.Order.Load;

                        acceptedOrders.Add(order.Order);

                        await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Order {order.Order.Id} accepted midway at {GetNodeName(nodeId)}, from {GetNodeName(order.Order.OriginNodeId)} to {GetNodeName(order.Order.TargetNodeId)}");
                        await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Loading order {order.Order.Id}");
                    }
                }
            }
        }

        private async Task GetOrders()
        {
            //Gets all available orders 
            _availableOrders = await hahnCargoSimClient.GetAvailableOrders(_token);

            //Filters the orders that are on the grid
            _availableOrders = _availableOrders.Where(o =>
                                                _grid.Nodes.Any(n => n.Id == o.TargetNodeId)
                                                &&
                                                _grid.Nodes.Any(n => n.Id == o.OriginNodeId)
                                                )
                                               .ToList();

            _acceptedOrders = await hahnCargoSimClient.GetAcceptedOrders(_token);
        }

        private double BestRouteIndex(TimeSpan time, int cost, int payment)
        {
            return (payment - cost) / time.TotalMinutes;
        }

        private string GetNodeName(int id)
        {
            //return _grid.Nodes.FirstOrDefault(n => n.Id == id).Id.ToString();
            return _grid.Nodes.FirstOrDefault(n => n.Id == id).Name;
        }

        private class Graph
        {
            private readonly List<Node> nodes;
            private readonly List<Edge> edges;
            private readonly List<Connection> connections;

            public Graph(List<Node> nodes, List<Edge> edges, List<Connection> connections)
            {
                this.nodes = nodes;
                this.edges = edges;
                this.connections = connections;
            }

            public RouteDto GetRoute(int startNodeId, int endNodeId)
            {
                var response = FindShortestPathNodes(startNodeId, endNodeId);

                var previousNodes = response.PreviousNodes;
                var time = response.Params.Time;
                var cost = response.Params.Cost;


                var path = new List<int>();
                int currentNodeId = endNodeId;
                while (currentNodeId != startNodeId)
                {
                    if (!previousNodes.ContainsKey(currentNodeId))
                    {
                        return null;
                    }

                    path.Add(currentNodeId);
                    currentNodeId = previousNodes[currentNodeId];
                }
                path.Add(startNodeId);
                path.Reverse();
                path.RemoveAt(0);

                return new RouteDto
                {
                    Route = path,
                    Time = time,
                    Cost = cost
                };
            }
            private BestPathDto FindShortestPathNodes(int startNodeId, int endNodeId)
            {
                var graph = new Dictionary<int, Dictionary<int, BestPathParamsDto>>();

                foreach (var connection in connections)
                {
                    var edge = edges.Find(e => e.Id == connection.EdgeId);
                    if (edge == null)
                    {
                        continue;
                    }
                    if (!graph.ContainsKey(connection.FirstNodeId))
                    {
                        graph[connection.FirstNodeId] = new Dictionary<int, BestPathParamsDto>();
                    }

                    graph[connection.FirstNodeId][connection.SecondNodeId] = new BestPathParamsDto { Time = edge.Time, Cost = edge.Cost };

                    if (!graph.ContainsKey(connection.SecondNodeId))
                    {
                        graph[connection.SecondNodeId] = new Dictionary<int, BestPathParamsDto>();
                    }

                    graph[connection.SecondNodeId][connection.FirstNodeId] = new BestPathParamsDto { Time = edge.Time, Cost = edge.Cost };
                }

                var shortestDistances = new Dictionary<int, BestPathParamsDto>();
                var previousNodes = new Dictionary<int, int>();
                var unvisitedNodes = new HashSet<int>();

                foreach (var node in nodes)
                {
                    shortestDistances[node.Id] = new BestPathParamsDto { Time = TimeSpan.MaxValue, Cost = int.MaxValue };
                    previousNodes[node.Id] = -1;
                    unvisitedNodes.Add(node.Id);
                }

                shortestDistances[startNodeId] = new BestPathParamsDto { Time = TimeSpan.Zero, Cost = 0 };

                while (unvisitedNodes.Count > 0)
                {
                    int currentNodeId = GetBestNode(unvisitedNodes, shortestDistances);

                    if (currentNodeId == -2)
                        break;

                    unvisitedNodes.Remove(currentNodeId);

                    if (currentNodeId == endNodeId)
                        break;

                    if (!graph.ContainsKey(currentNodeId))
                        continue;

                    foreach (var neighbor in graph[currentNodeId])
                    {
                        TimeSpan tentativeDistance = shortestDistances[currentNodeId].Time + neighbor.Value.Time;
                        int tentativeCost = shortestDistances[currentNodeId].Cost + neighbor.Value.Cost;
                        if (tentativeDistance < shortestDistances[neighbor.Key].Time)
                        {
                            shortestDistances[neighbor.Key] = new BestPathParamsDto { Time = tentativeDistance, Cost = tentativeCost };
                            previousNodes[neighbor.Key] = currentNodeId;
                        }
                    }
                }

                return new BestPathDto
                {
                    PreviousNodes = previousNodes,
                    Params = new BestPathParamsDto
                    {
                        Time = shortestDistances[endNodeId].Time,
                        Cost = shortestDistances[endNodeId].Cost
                    }
                };

            }

            private int GetBestNode(HashSet<int> unvisitedNodes, Dictionary<int, BestPathParamsDto> shortestDistances)
            {
                int closestNodeId = -1;
                TimeSpan shortestTime = TimeSpan.MaxValue;
                foreach (var nodeId in unvisitedNodes)
                {
                    if (shortestDistances[nodeId].Time < shortestTime)
                    {
                        closestNodeId = nodeId;
                        shortestTime = shortestDistances[nodeId].Time;
                    }
                }

                if (closestNodeId == -1 && unvisitedNodes.Count > 0)
                {
                    return -2;
                }

                return closestNodeId;
            }
        }
    }
}

