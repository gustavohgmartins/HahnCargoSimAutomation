using App.Core.Clients;
using App.Domain.DTOs;
using App.Domain.Model;
using App.Domain.Services;
using QuickGraph;
using QuickGraph.Algorithms.ShortestPath;
using System.Linq;


namespace App.Core.Services
{
    public class Automation : IAutomation
    {
        private readonly HahnCargoSimClient hahnCargoSimClient;
        private readonly AuthDto authUser;
        private string _token;

        private bool _isRunning;

        public Automation(HahnCargoSimClient hahnCargoSimClient, AuthDto authUser)
        {
            this.hahnCargoSimClient = hahnCargoSimClient;
            this.authUser = authUser;
        }

        public async Task Start(string token)
        {
            _token = token;

            ////if (_isRunning)
            ////{
            ////    return;
            ////}

            _isRunning = true;
            Task.Run(ExecuteAsync);
        }

        public async Task Stop()
        {
            _isRunning = false;
        }

        private async Task ExecuteAsync()
        {
            var grid = await hahnCargoSimClient.GetGrid(_token);

            var start = 15;
            var end = 1;

            ////while (_isRunning)
            ////{
            var coins = await hahnCargoSimClient.GetCoinAmount(_token);
            Console.WriteLine($"Hello, {authUser.Username}! You have {coins} coins!");

            //////////////////////

            
            ////UndirectedGraph<Node, Edge<Node>> graph = new UndirectedGraph<Node, Edge<Node>>();
            ////graph.AddVertexRange(grid.Nodes.ToArray());
           
            ////grid.Connections.Select(c => graph.AddEdge(new Edge<Node>(grid.Nodes.First(n => n.Id == c.FirstNodeId), grid.Nodes.First(n => n.Id == c.SecondNodeId))));

            ////////Func<Edge<Node>, int> edgeCost = edge => grid.Edges.First(e => e.Id == (grid.Connections.Where(c => (c.FirstNodeId == edge.Source.Id || c.FirstNodeId == edge.Target.Id) && (c.SecondNodeId == edge.Source.Id || c.SecondNodeId == edge.Target.Id)).First().EdgeId)).Cost;
            ////Func<Edge<Node>, int> edgeCost = edge => 1;
            
            ////var dijkstra = new UndirectedDijkstraShortestPathAlgorithm<Node, Edge<Node>>(graph, edge => edgeCost(edge));

            ////dijkstra.SetRootVertex(graph.Vertices.First(v => v.Id == start));
            
            ////dijkstra.Compute();

            ////double distance;

            ////var targetVertex = graph.Vertices.First(v => v.Id == end);

            ////bool found = dijkstra.TryGetDistance(targetVertex, out distance);

            ////if (found)
            ////{
            ////    Console.WriteLine($"Distância de {graph.Vertices.First(v => v.Id == start).Name} para {targetVertex.Name}: {distance}");
            ////}
            ////else
            ////{
            ////    Console.WriteLine($"Não foi possível alcançar {targetVertex} a partir de A.");
            ////}

            //////////////////

            var teste = new Graph(grid.Nodes, grid.Edges, grid.Connections);

            var previousNodes = teste.FindShortestPath(start, end);
            var path = teste.ReconstructPath(start, end, previousNodes);

            if (path != null)
            {
                Console.WriteLine("Caminho mais curto encontrado:");
                foreach (var nodeId in path)
                {
                    var node = grid.Nodes.Find(n => n.Id == nodeId);
                    Console.WriteLine($"-> {node.Id}");
                }
            }
            else
            {
                Console.WriteLine("Não há caminho entre os nós especificados.");
                Console.WriteLine($"{start} -> {end}");
            }

            await Task.Delay(100000);
        }
    ////}
    }

    public class Graph
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

        public Dictionary<int, int> FindShortestPath(int startNodeId, int endNodeId)
        {
            var graph = new Dictionary<int, Dictionary<int, int>>();

            foreach (var connection in connections)
            {
                var edge = edges.Find(e => e.Id == connection.EdgeId);
                if (edge == null)
                {
                    continue;
                }
                if (!graph.ContainsKey(connection.FirstNodeId))
                    graph[connection.FirstNodeId] = new Dictionary<int, int>();
                graph[connection.FirstNodeId][connection.SecondNodeId] = edge.Cost;

                if (!graph.ContainsKey(connection.SecondNodeId))
                    graph[connection.SecondNodeId] = new Dictionary<int, int>();
                graph[connection.SecondNodeId][connection.FirstNodeId] = edge.Cost;
            }

            var shortestDistances = new Dictionary<int, int>();
            var previousNodes = new Dictionary<int, int>();
            var unvisitedNodes = new HashSet<int>();

            foreach (var node in nodes)
            {
                shortestDistances[node.Id] = int.MaxValue;
                previousNodes[node.Id] = -1;
                unvisitedNodes.Add(node.Id);
            }

            shortestDistances[startNodeId] = 0;

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
                    int tentativeDistance = shortestDistances[currentNodeId] + neighbor.Value;
                    if (tentativeDistance < shortestDistances[neighbor.Key])
                    {
                        shortestDistances[neighbor.Key] = tentativeDistance;
                        previousNodes[neighbor.Key] = currentNodeId;
                    }
                }
            }

            return previousNodes;
        }

        private int GetClosestNode(HashSet<int> unvisitedNodes, Dictionary<int, int> shortestDistances)
        {
            int closestNodeId = -1;
            int shortestDistance = int.MaxValue;
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

        public List<int> ReconstructPath(int startNodeId, int endNodeId, Dictionary<int, int> previousNodes)
        {
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
            return path;
        }
    }
}

