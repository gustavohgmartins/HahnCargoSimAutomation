using App.Core.Clients;
using App.Core.Hubs;
using App.Domain.DTOs;
using App.Domain.Model;
using App.Domain.Models;
using App.Domain.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Core.Services
{
    public class Automation : IAutomation
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        private readonly AuthDto authUser;
        private readonly AutomationHub hub; // SignalR hub to send logs to the front-end real-time
        private readonly Consumer consumer;
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
        private List<int> _ordersToRemove = new List<int>(); // Orders that were accepted by someone (needed when consuming strictly from RabbitMQ)
        private Dictionary<int, Order> _transporterAcceptedOrder = new Dictionary<int, Order>(); // Links an accepted order to an unloaded transporter indicating his next destination.

        public Automation(HahnCargoSimClient hahnCargoSimClient, AuthDto authUser, IConfiguration configuration, AutomationHub hub, Consumer consumer)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
            this.authUser = authUser;
            this.configuration = configuration;
            this.hub = hub;
            this.consumer = consumer;
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

            await hub.SendLog(authUser.Username, "isRunning", "1");

            Task.Run(ExecuteAsync);
        }

        public async Task Stop()
        {
            _isRunning = false;
            await hub.SendLog(authUser.Username, "isRunning", "0");
            await hub.SendLog(authUser.Username, "Simulation", "Simulation stopped");
        }

        private async Task ExecuteAsync()
        {
            try
            {
                _grid = await hahnCargoSimClient.GetGrid(_token);
                _graph = new Graph(_grid.Nodes, _grid.Edges, _grid.Connections);

                await hub.SendLog(authUser.Username, "Simulation", "Simulation started");

                while (_isRunning)
                {
                    _coins = await hahnCargoSimClient.GetCoinAmount(_token);
                    await hub.SendLog(authUser.Username, "coins", _coins.ToString());
                    await hub.SendLog(authUser.Username, "transporters", _transportersIds.Count.ToString());
                    await hub.SendLog(authUser.Username, "isRunning", "1");

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

                    await BuyTransporter();
                    await GetTransporters();
                    await BuildRoutes();
                    await MoveTransporters();

                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await hub.SendLog(authUser.Username, "Simulation", "ERROR | " + e.Message);
            }
        }

        private async Task BuyTransporter()
        {
            try
            {
                if (_maxTransporters > 0 && _transporters.Count() >= _maxTransporters)
                {
                    return;
                }

                if (!_transportersIds.Any() || _coins > (1000 * (1 + 0.1 * _transporters.Count)))
                {
                    var bestOrder = _availableOrders
                        .Select(o => new
                        {
                            OrderRoute = _graph.GetBestRoute(o.OriginNodeId, o.TargetNodeId),
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
                    _ordersToRemove.Add(bestOrder.Order.Id);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await hub.SendLog(authUser.Username, "Simulation", "BuyTransporters | ERROR | " + e);
            }
        }

        private async Task GetTransporters()
        {
            if (_transportersIds.Any())
            {
                ManageTransportersAcceptedOrders();

                _transporters = [];

                foreach (var id in _transportersIds)
                {
                    try
                    {
                        var transporter = await hahnCargoSimClient.GetCargoTransporter(_token, id);
                        if (transporter is not null)
                        {
                            if (_transporterAcceptedOrder.ContainsKey(transporter.Id))
                            {
                                transporter.AcceptedOrder = _transporterAcceptedOrder[transporter.Id];
                            }

                            transporter.Route = [];

                            _transporters.Add(transporter);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        await hub.SendLog(authUser.Username, "Simulation", "GetTransporters | ERROR | " + e.Message);
                    }
                }
            }
        }

        private async Task BuildRoutes()
        {
            var notInTransitTransporters = _transporters.Where(t => !t.InTransit).ToList();

            foreach (var transporter in notInTransitTransporters)
            {
                try
                {
                    if (transporter.LoadedOrders.Any())
                    {
                        var bestOrder = transporter.LoadedOrders
                            .Select(o => new
                            {
                                Route = _graph.GetBestRoute(transporter.PositionNodeId, o.TargetNodeId),
                                Order = o
                            })
                            .OrderBy(r => r.Route.Time)
                            .First();

                        transporter.Route = bestOrder.Route.Route;

                        await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Route updated. Current route: {string.Join(" -> ", transporter.Route.Select(r => GetNodeName(r)).ToList())} | Currently on route to delivery order {bestOrder.Order.Id} | Estimated time for delivery: {bestOrder.Route.Time}");
                    }
                    else if (transporter.AcceptedOrder is not null)
                    {
                        var route = _graph.GetBestRoute(transporter.PositionNodeId, transporter.AcceptedOrder.OriginNodeId);

                        transporter.Route = route.Route;

                        if (route.Time != TimeSpan.Zero)
                        {
                            await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Route updated. Current route: {string.Join(" -> ", transporter.Route.Select(r => GetNodeName(r)).ToList())} | Currently on route to pick up order {transporter.AcceptedOrder.Id} | Estimated time for pick up: {route.Time}");
                        }
                    }
                    else // Accepts the next order and sets the transporter on route to pick it up
                    {
                        var bestOrder = _availableOrders
                            .Select(o => new
                            {
                                OrderRoute = _graph.GetBestRoute(o.OriginNodeId, o.TargetNodeId),
                                PickUpRoute = _graph.GetBestRoute(transporter.PositionNodeId, o.OriginNodeId),
                                Order = o
                            })
                            .OrderByDescending(r => BestRouteIndex(r.OrderRoute.Time + r.PickUpRoute.Time, r.OrderRoute.Cost + r.PickUpRoute.Cost, r.Order.Value))
                            .First();

                        var orderAccepted = await hahnCargoSimClient.AcceptOrder(_token, bestOrder.Order.Id);

                        if (orderAccepted)
                        {
                            await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Order {bestOrder.Order.Id} accepted from {GetNodeName(bestOrder.Order.OriginNodeId)} to {GetNodeName(bestOrder.Order.TargetNodeId)}");

                            // Sets the accepted order to the current transporter
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

                        _ordersToRemove.Add(bestOrder.Order.Id);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    await hub.SendLog(authUser.Username, "Simulation", "BuildRoutes | ERROR | " + e.Message);
                }
            }
        }

        private async Task MoveTransporters()
        {
            var transportersToMove = _transporters.Where(t => t.Route.Any() && !t.InTransit).ToList();

            foreach (var transporter in transportersToMove)
            {
                try
                {
                    var targetNode = transporter.Route[0];

                    var moveTransporter = await hahnCargoSimClient.MoveCargoTransporter(_token, transporter.Id, targetNode);

                    if (moveTransporter)
                    {
                        await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Moving from {GetNodeName(transporter.PositionNodeId)} to {GetNodeName(targetNode)}");

                        await AcceptNodeOrders(transporter, targetNode);//Accepts any acceptable order in the target node.

                        transporter.Route.RemoveAt(0);

                        if (!transporter.Route.Any())
                        {
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
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    await hub.SendLog(authUser.Username, "Simulation", "MoveTransporters | ERROR | " + e.Message);
                }
            }
        }

        private async Task AcceptNodeOrders(CargoTransporter transporter, int nodeId)
        {
            try
            {
                if (transporter.LoadedOrders.Any(o => OrderAboutToExpire(o)) || OrderAboutToExpire(transporter.AcceptedOrder)) // If the transporter has any order about to expire it ignores the available orders
                {
                    return;
                }

                var nodeAvailableOrders = _availableOrders
                        .Where(o => o.OriginNodeId == nodeId)
                        .Select(o => new
                        {
                            OrderRoute = _graph.GetBestRoute(o.OriginNodeId, o.TargetNodeId),
                            Order = o
                        })
                        .OrderByDescending(r => BestRouteIndex(r.OrderRoute.Time, r.OrderRoute.Cost, r.Order.Value));

                var currentLoad = transporter.Load + (transporter.AcceptedOrder?.Load ?? 0);

                foreach (var order in nodeAvailableOrders)
                {
                    try
                    {
                        if (currentLoad + order.Order.Load <= transporter.Capacity)
                        {
                            var orderAccepted = await hahnCargoSimClient.AcceptOrder(_token, order.Order.Id);

                            if (orderAccepted)
                            {
                                currentLoad += order.Order.Load;

                                await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Order {order.Order.Id} accepted midway at {GetNodeName(nodeId)}, from {GetNodeName(order.Order.OriginNodeId)} to {GetNodeName(order.Order.TargetNodeId)}");
                                await hub.SendLog(authUser.Username, $"Transporter {transporter.Id}", $"Loading order {order.Order.Id}");
                            }
                            _ordersToRemove.Add(order.Order.Id);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        await hub.SendLog(authUser.Username, "Simulation", "AcceptNodeOrders/nodeAvailableOrders | ERROR | " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await hub.SendLog(authUser.Username, "Simulation", "AcceptNodeOrders | ERROR | " + e.Message);
            }
        }


        /// <summary>
        ///  Removes from _transporterAcceptedOrder the orders that were already picked up.
        /// </summary>
        private void ManageTransportersAcceptedOrders()
        {
            List<int> transporterAcceptedOrdersToRemove = new List<int>();

            foreach (var obj in _transporterAcceptedOrder)
            {
                foreach (var transporter in _transporters)
                {
                    if (transporter.LoadedOrders.Any(l => l.Id == obj.Value.Id))
                    {
                        transporterAcceptedOrdersToRemove.Add(transporter.Id);
                    }
                }
            }

            foreach (var transporterId in transporterAcceptedOrdersToRemove)
            {
                _transporterAcceptedOrder.Remove(transporterId);
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

        private async Task GetOrders()
        {
            try
            {
                _acceptedOrders = await hahnCargoSimClient.GetAcceptedOrders(_token);

                consumer._availableOrders = consumer._consumedOrders.ToList();
                consumer._availableOrders.RemoveAll(o => _ordersToRemove.Contains(o.Id));

                // Filters the consumed orders that are not expired and on the grid
                consumer._availableOrders.RemoveAll(o => DateTime.ParseExact(o.ExpirationDateUtc, "MM/dd/yyyy HH:mm:ss", null) <= DateTime.UtcNow
                                                        || !_grid.Nodes.Any(n => n.Id == o.TargetNodeId)
                                                        || !_grid.Nodes.Any(n => n.Id == o.OriginNodeId));

                _availableOrders = consumer._availableOrders;

                if (!_availableOrders.Any()) // If the consumer is not consuming
                {
                    // Easier way
                    // Gets the available orders directly from the GetAllAvailable endpoint
                    _availableOrders = await hahnCargoSimClient.GetAvailableOrders(_token);
                    _availableOrders.RemoveAll(o => !_grid.Nodes.Any(n => n.Id == o.TargetNodeId) || !_grid.Nodes.Any(n => n.Id == o.OriginNodeId));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await hub.SendLog(authUser.Username, "Simulation", "GetOrders | ERROR | " + e.Message);
            }
        }

        /// <summary>
        /// Calculates coins per minute using time, cost, and payment attributed to an order.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="cost"></param>
        /// <param name="payment"></param>
        /// <returns></returns>
        private double BestRouteIndex(TimeSpan time, int cost, int payment)
        {
            return (payment - cost) / time.TotalMinutes;
        }

        private string GetNodeName(int id)
        {
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

            //// Builds the best path based on the FindBestPathNodes calculation result
            public RouteDto GetBestRoute(int startNodeId, int endNodeId)
            {
                var response = FindBestPathNodes(startNodeId, endNodeId);

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

            //// Dijkstra algorithm customized to fit the current needs (currently calculating the 'best' path based strictly on time)
            private BestPathDto FindBestPathNodes(int startNodeId, int endNodeId)
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