using App.Core.Clients;
using App.Domain.DTOs;
using App.Domain.Model;
using App.Domain.Models;
using App.Domain.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using QuickGraph;
using QuickGraph.Algorithms.ShortestPath;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace App.Core.Services
{
    public class Automation : IAutomation
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        private readonly AuthDto authUser;
        private string _token;
        private bool _isRunning;
        private Grid _grid;
        private Graph _graph;
        private List<Order> _availableOrders = new List<Order>();
        private List<Order> _acceptedOrders = new List<Order>();
        private List<CargoTransporter> _transporters = new List<CargoTransporter>();
        private List<int> _transportersIds = new List<int>();
        private Dictionary<int, Order> _transporterAcceptedOrder = new Dictionary<int, Order>();
        private int _coins;
        private List<double> _teste = new List<double>();

        public Automation(HahnCargoSimClient hahnCargoSimClient, AuthDto authUser)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
            this.authUser = authUser;
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

            Console.WriteLine($"\nHello, {authUser.Username}!");

            Console.WriteLine($"\nSimulation started");

            while (_isRunning)
            {
                _coins = await hahnCargoSimClient.GetCoinAmount(_token);

                await GetOrders();

                if (!_availableOrders.Any() && !_acceptedOrders.Any())
                {
                    Console.WriteLine($"\nWaiting for available orders...");

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

            Console.WriteLine("Finished");
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
            ////var currentNodeAcceptedOrders = await AcceptCurrentNodeOrders(transporter);//Checks if there are any acceptable orders in the transporter current node

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

                Console.WriteLine($"\nRoute updated for transporter {transporter.Id} current route: {string.Join(", ", transporter.Route.Select(r => GetNodeName(r)).ToList())} | Currently on route to delivery order {bestOrder.Order.Id} | Estimated time for delivery: {bestOrder.Route.Time}");
            }
            else if (transporter.AcceptedOrder is not null)
            {
                var route = _graph.GetRoute(transporter.PositionNodeId, transporter.AcceptedOrder.OriginNodeId);
                transporter.Route = route.Route;

                if (route.Time != TimeSpan.Zero)
                {
                    Console.WriteLine($"\nRoute updated for transporter {transporter.Id} current route: {string.Join(", ", transporter.Route.Select(r => GetNodeName(r)).ToList())} | Currently on route to pick up order {transporter.AcceptedOrder.Id} | Estimated time for pick up: {route.Time}");
                }
            }
            ////Accepts the next order and sets the transporter route to pick it up
            else
            {
                //if (!currentNodeAcceptedOrders.Any())
                //{
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
                    Console.WriteLine($"\nOrder {bestOrder.Order.Id} accepted from {GetNodeName(bestOrder.Order.OriginNodeId)} to {GetNodeName(bestOrder.Order.TargetNodeId)}");

                    ////Set the accepted order to the current transporter
                    _transporterAcceptedOrder[transporter.Id] = bestOrder.Order;

                    transporter.AcceptedOrder = bestOrder.Order;

                    transporter.Route = bestOrder.PickUpRoute.Route;

                    Console.WriteLine($"\nRoute updated for transporter {transporter.Id} current route: {string.Join(", ", transporter.Route)} | Currently on route to pick up order {bestOrder.Order.Id}  Estimated time for pick up: {bestOrder.PickUpRoute.Time}");
                }
                //}
            }

            return transporter;
        }

        private async Task BuyTransporter()
        {
            ////if (!_transportersIds.Any() || _coins > (1000 * (1 + 0.1 * _transporters.Count)))

            if (!_transportersIds.Any())
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
                    Console.WriteLine($"\nOrder {bestOrder.Order.Id} accepted from {GetNodeName(bestOrder.Order.OriginNodeId)} to {GetNodeName(bestOrder.Order.TargetNodeId)}. Estimated delivery time {bestOrder.OrderRoute.Time}");

                    var transporterId = await hahnCargoSimClient.BuyCargoTransporter(_token, bestOrder.Order.OriginNodeId);

                    _transportersIds.Add(transporterId);

                    _transporterAcceptedOrder[transporterId] = bestOrder.Order;

                    Console.WriteLine($"\nTransporter {transporterId} bought at {GetNodeName(bestOrder.Order.OriginNodeId)}");
                    Console.WriteLine($"\nTransporter {transporterId} loading order {bestOrder.Order.Id}");
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
                    Console.WriteLine($"\nMoving transporter {transporter.Id} from {GetNodeName(transporter.PositionNodeId)} to {GetNodeName(targetNode)}");
                    transporter.Route.RemoveAt(0);

                    if (!transporter.Route.Any())
                    {
                        _transporterAcceptedOrder.Remove(transporter.Id);
                        if (transporter.LoadedOrders.Any(o => o.TargetNodeId == targetNode))
                        {
                            Console.WriteLine($"\nTransporter {transporter.Id} delivering the following orders: {string.Join(", ", transporter.LoadedOrders.Where(o => o.TargetNodeId == targetNode))}");
                        }
                        else
                        {
                            Console.WriteLine($"\nTransporter {transporter.Id} picking up the following order: {transporter.AcceptedOrder.I}");
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
        private async Task<List<Order>> AcceptCurrentNodeOrders(CargoTransporter transporter)
        {
            if (transporter.LoadedOrders.Any(o => OrderAboutToExpire(o)) || OrderAboutToExpire(transporter.AcceptedOrder))
            {
                return default;
            }

            List<Order> acceptedOrders = new List<Order>();
            var currentNodeAvailableOrders = _availableOrders
                    .Where(o => o.OriginNodeId == transporter.PositionNodeId)
                    .Select(o => new
                    {
                        OrderRoute = _graph.GetRoute(o.OriginNodeId, o.TargetNodeId),
                        Order = o
                    })
                    .OrderByDescending(r => BestRouteIndex(r.OrderRoute.Time, r.OrderRoute.Cost, r.Order.Value));

            foreach (var order in currentNodeAvailableOrders)
            {
                if (transporter.Load + transporter.AcceptedOrder?.Load + order.Order.Load <= transporter.Capacity)
                {
                    var orderAccepted = await hahnCargoSimClient.AcceptOrder(_token, order.Order.Id);

                    if (orderAccepted)
                    {
                        transporter.Load += order.Order.Load;

                        acceptedOrders.Add(order.Order);

                        Console.WriteLine($"\nOrder {order.Order.Id} accepted from {GetNodeName(order.Order.OriginNodeId)} to {GetNodeName(order.Order.TargetNodeId)}");
                    }
                }
            }

            return acceptedOrders;
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
            return _grid.Nodes.FirstOrDefault(n => n.Id == id).Id.ToString();
            ////return _grid.Nodes.FirstOrDefault(n => n.Id == id).Name;
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
                        return null;

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
                //    var graph = new Dictionary<int, Dictionary<int, TimeSpan>>();

                //    foreach (var connection in connections)
                //    {
                //        var edge = edges.Find(e => e.Id == connection.EdgeId);
                //        if (edge == null)
                //        {
                //            continue;
                //        }
                //        if (!graph.ContainsKey(connection.FirstNodeId))
                //            graph[connection.FirstNodeId] = new Dictionary<int, TimeSpan>();
                //        graph[connection.FirstNodeId][connection.SecondNodeId] = edge.Time;

                //        if (!graph.ContainsKey(connection.SecondNodeId))
                //            graph[connection.SecondNodeId] = new Dictionary<int, TimeSpan>();
                //        graph[connection.SecondNodeId][connection.FirstNodeId] = edge.Time;
                //    }

                //    var shortestDistances = new Dictionary<int, TimeSpan>();
                //    var previousNodes = new Dictionary<int, int>();
                //    var unvisitedNodes = new HashSet<int>();

                //    foreach (var node in nodes)
                //    {
                //        shortestDistances[node.Id] = TimeSpan.MaxValue;
                //        previousNodes[node.Id] = -1;
                //        unvisitedNodes.Add(node.Id);
                //    }

                //    shortestDistances[startNodeId] = TimeSpan.Zero;

                //    while (unvisitedNodes.Count > 0)
                //    {
                //        int currentNodeId = GetClosestNode(unvisitedNodes, shortestDistances);

                //        if (currentNodeId == -2)
                //            break;

                //        unvisitedNodes.Remove(currentNodeId);

                //        if (currentNodeId == endNodeId)
                //            break;

                //        if (!graph.ContainsKey(currentNodeId))
                //            continue;

                //        foreach (var neighbor in graph[currentNodeId])
                //        {
                //            TimeSpan tentativeDistance = shortestDistances[currentNodeId] + neighbor.Value;
                //            if (tentativeDistance < shortestDistances[neighbor.Key])
                //            {
                //                shortestDistances[neighbor.Key] = tentativeDistance;
                //                previousNodes[neighbor.Key] = currentNodeId;
                //            }
                //        }
                //    }

                //    return new BestPathDto
                //    {
                //        PreviousNodes = previousNodes,
                //        Time = shortestDistances[endNodeId],
                //    };

                var graph = new Dictionary<int, Dictionary<int, BestPathParamsDto>>();

                foreach (var connection in connections)
                {
                    var edge = edges.Find(e => e.Id == connection.EdgeId);
                    if (edge == null)
                    {
                        continue;
                    }
                    if (!graph.ContainsKey(connection.FirstNodeId))
                        graph[connection.FirstNodeId] = new Dictionary<int, BestPathParamsDto>();
                    graph[connection.FirstNodeId][connection.SecondNodeId] = new BestPathParamsDto { Time = edge.Time, Cost = edge.Cost };

                    if (!graph.ContainsKey(connection.SecondNodeId))
                        graph[connection.SecondNodeId] = new Dictionary<int, BestPathParamsDto>();
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

