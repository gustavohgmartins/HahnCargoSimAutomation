using App.Core.Clients;
using App.Domain.DTOs;
using App.Domain.Model;
using App.Domain.Models;
using App.Domain.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using QuickGraph;
using QuickGraph.Algorithms.ShortestPath;
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
        int _coins;

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
            Console.WriteLine($"\nWaiting for available orders...");

            while (!_availableOrders.Any())
            {
                _availableOrders = await hahnCargoSimClient.GetAvailableOrders(_token);
                await Task.Delay(1000);
            }
            Console.WriteLine($"\nStarted");

            while (_isRunning)
            {
                _coins = await hahnCargoSimClient.GetCoinAmount(_token);

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
                if (!_availableOrders.Any() && !_acceptedOrders.Any())
                {
                    Console.WriteLine($"\nWaiting for available orders...");
                }

                //Gets the transporters
                _transporters = [];
                if (_transportersIds.Any())
                {
                    foreach (var id in _transportersIds)
                    {
                        var transporter = await hahnCargoSimClient.GetCargoTransporter(_token, id);
                        if (transporter is not null)
                        {
                            transporter.Route = [];

                            if (!transporter.InTransit)
                            {
                                transporter = await BuildRoute(transporter);
                            }

                            _transporters.Add(transporter);
                        }
                    }
                }
                ////Buys the first transporter at the first order start location
                else if (_availableOrders.Any())
                {
                    ////Starts accepting the quicker route from the first 100 options
                    var orderToAccept = _availableOrders
                        .Select(o => new
                        {
                            RouteDto = _graph.GetRoute(o.OriginNodeId, o.TargetNodeId),
                            Order = o
                        })
                        .OrderBy(r => r.RouteDto.Time)
                        .First();

                    ////var time = TimeSpan.MaxValue;
                    ////var orderToAccept = 0;
                    ////foreach (var order in _availableOrders.Take(100))
                    ////{
                    ////    var route = _graph.GetRoute(order.OriginNodeId, order.TargetNodeId);      WHEN EVERY DETAIL FOR PERFORMANCE ENHANCING COUNTS
                    ////    if (route.Time < time)
                    ////    {
                    ////        time = route.Time;
                    ////        orderToAccept = order.Id;

                    ////    }
                    ////}

                    var orderAccepted = await hahnCargoSimClient.AcceptOrder(_token, orderToAccept.Order.Id);

                    if (orderAccepted)
                    {
                        Console.WriteLine($"\nOrder {orderToAccept.Order.Id} accepted from {GetNodeName(orderToAccept.Order.OriginNodeId)} to {GetNodeName(orderToAccept.Order.TargetNodeId)}. Estimated delivery time {orderToAccept.RouteDto.Time}");

                        _transportersIds = [await hahnCargoSimClient.BuyCargoTransporter(_token, orderToAccept.Order.OriginNodeId)];

                        Console.WriteLine($"\nTransporter {_transportersIds.First()} bought at {GetNodeName(orderToAccept.Order.OriginNodeId)}");
                        Console.WriteLine($"\nTransporter {_transportersIds.First()} loading order {orderToAccept.Order.Id}");
                    }
                }


                ////Moves transporters
                foreach (var transporter in _transporters.Where(t => t.Route.Any() && !t.InTransit))
                {
                    var targetNode = transporter.Route[0];
                    var moveTransporter = await hahnCargoSimClient.MoveCargoTransporter(_token, transporter.Id, targetNode);
                    if (moveTransporter)
                    {
                        Console.WriteLine($"\nMoving transporter {transporter.Id} from {GetNodeName(transporter.PositionNodeId)} to {GetNodeName(targetNode)}");
                        transporter.Route.RemoveAt(0);

                        if (!transporter.Route.Any())
                        {
                            Console.WriteLine($"\nTransporter {transporter.Id} Delivering the following orders: {string.Join(", ", transporter.LoadedOrders.Where(o => o.TargetNodeId == targetNode).Select(o => o.Id))}");
                        }
                    }
                }

                await Task.Delay(2000);
            }
            Console.WriteLine("Finished");
        }

        //Build the best route for the transporter, at the moment
        private async Task<CargoTransporter> BuildRoute(CargoTransporter transporter)
        {
            if (transporter.LoadedOrders.Any())
            {
                var closestOrder = transporter.LoadedOrders
                    .Select(o => new
                    {
                        RouteDto = _graph.GetRoute(transporter.PositionNodeId, o.TargetNodeId),
                        Order = o
                    })
                    .OrderBy(r => r.RouteDto.Time)
                    .First();
                transporter.Route = closestOrder.RouteDto.Route;

                Console.WriteLine($"\nRoute updated for transporter {transporter.Id} current route: {string.Join(", ", transporter.Route.Select(r => GetNodeName(r)).ToList())} | Currently on route to delivery order {closestOrder.Order.Id} | Estimated time for delivery: {closestOrder.RouteDto.Time}");
            }
            else if (_acceptedOrders.Any())
            {
                var closestOrder = _acceptedOrders
                    .Select(o => new
                    {
                        RouteDto = _graph.GetRoute(transporter.PositionNodeId, o.OriginNodeId),
                        Order = o
                    })
                    .OrderBy(r => r.RouteDto.Time)
                    .First();

                transporter.Route = closestOrder.RouteDto.Route;
                if (closestOrder.RouteDto.Time != TimeSpan.Zero)
                {
                    Console.WriteLine($"\nRoute updated for transporter {transporter.Id} current route: {string.Join(", ", transporter.Route)} | Currently on route to pick up order {closestOrder.Order.Id} | Estimated time for pick up: {closestOrder.RouteDto.Time}");
                }

            }
            ////Accepts the next order and sets the transporter route to pick it up
            else
            {
                ////List<RouteDto> availableOrdersRoutes = new List<RouteDto>();

                ////foreach (var order in _availableOrders)
                ////{
                ////    availableOrdersRoutes.Add(_graph.GetRoute(transporter.PositionNodeId, order.OriginNodeId));     WHEN EVERY DETAIL FOR PERFORMANCE ENHANCING COUNTS
                ////}

                ////var route = availableOrdersRoutes.OrderBy(t => t.Time).First();

                var closestOrder = _availableOrders
                    .Select(o => new
                    {
                        RouteDto = _graph.GetRoute(transporter.PositionNodeId, o.OriginNodeId),
                        Order = o
                    })
                    .OrderBy(r => r.RouteDto.Time)
                    .First();

                var orderAccepted = await hahnCargoSimClient.AcceptOrder(_token, closestOrder.Order.Id);

                if (orderAccepted)
                {
                    transporter.Route = closestOrder.RouteDto.Route;

                    Console.WriteLine($"\nRoute updated for transporter {transporter.Id} current route: {string.Join(", ", transporter.Route)} | Currently on route to pick up order {closestOrder.Order.Id}  Estimated time for delivery: {closestOrder.RouteDto.Time}");
                }
            }

            return transporter;
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

            public RouteDto GetRoute(int startNodeId, int endNodeId)
            {
                var response = FindShortestPathNodes(startNodeId, endNodeId);

                var previousNodes = response.PreviousNodes;
                var TotalCost = response.TotalCost;

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
                    Time = TotalCost
                };
            }

            private int GetClosestNode(HashSet<int> unvisitedNodes, Dictionary<int, TimeSpan> shortestDistances)
            {
                int closestNodeId = -1;
                TimeSpan shortestDistance = TimeSpan.MaxValue;
                foreach (var nodeId in unvisitedNodes)
                {
                    if (shortestDistances[nodeId] < shortestDistance)
                    {
                        closestNodeId = nodeId;
                        shortestDistance = shortestDistances[nodeId];
                    }
                }

                if (closestNodeId == -1 && unvisitedNodes.Count > 0)
                {
                    return -2;
                }

                return closestNodeId;
            }

            private ShortestPathDto FindShortestPathNodes(int startNodeId, int endNodeId)
            {
                var graph = new Dictionary<int, Dictionary<int, TimeSpan>>();

                foreach (var connection in connections)
                {
                    var edge = edges.Find(e => e.Id == connection.EdgeId);
                    if (edge == null)
                    {
                        continue;
                    }
                    if (!graph.ContainsKey(connection.FirstNodeId))
                        graph[connection.FirstNodeId] = new Dictionary<int, TimeSpan>();
                    graph[connection.FirstNodeId][connection.SecondNodeId] = edge.Time;

                    if (!graph.ContainsKey(connection.SecondNodeId))
                        graph[connection.SecondNodeId] = new Dictionary<int, TimeSpan>();
                    graph[connection.SecondNodeId][connection.FirstNodeId] = edge.Time;
                }

                var shortestDistances = new Dictionary<int, TimeSpan>();
                var previousNodes = new Dictionary<int, int>();
                var unvisitedNodes = new HashSet<int>();

                foreach (var node in nodes)
                {
                    shortestDistances[node.Id] = TimeSpan.MaxValue;
                    previousNodes[node.Id] = -1;
                    unvisitedNodes.Add(node.Id);
                }

                shortestDistances[startNodeId] = TimeSpan.Zero;

                while (unvisitedNodes.Count > 0)
                {
                    int currentNodeId = GetClosestNode(unvisitedNodes, shortestDistances);

                    if (currentNodeId == -2)
                        break;

                    unvisitedNodes.Remove(currentNodeId);

                    if (currentNodeId == endNodeId)
                        break;

                    if (!graph.ContainsKey(currentNodeId))
                        continue;

                    foreach (var neighbor in graph[currentNodeId])
                    {
                        TimeSpan tentativeDistance = shortestDistances[currentNodeId] + neighbor.Value;
                        if (tentativeDistance < shortestDistances[neighbor.Key])
                        {
                            shortestDistances[neighbor.Key] = tentativeDistance;
                            previousNodes[neighbor.Key] = currentNodeId;
                        }
                    }
                }
                return new ShortestPathDto
                {
                    PreviousNodes = previousNodes,
                    TotalCost = shortestDistances[endNodeId]
                };
            }
        }
    }
}

