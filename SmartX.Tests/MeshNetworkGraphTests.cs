
using SmartX.Shared.Networking;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartX.Tests
{
    public class MeshNetworkGraphTests
    {
        private MeshNetWorkGraph CreateTestGraph()
        {
            MeshNetWorkGraph graph = new();

            graph.AddDevice("Gateway");
            graph.AddDevice("Sensor A");
            graph.AddDevice("Sensor B");
            graph.AddDevice("Sensor C");

            graph.AddConnection("Gateway", "Sensor A", 5);
            graph.AddConnection("Gateway", "Sensor B", 8);
            graph.AddConnection("Sensor A", "Sensor B", 2);
            graph.AddConnection("Sensor A", "Sensor C", 6);
            graph.AddConnection("Sensor B", "Sensor C", 3);

            return graph;
        }

        [Fact]
        public void AddConnection_ShouldConnectBothDevices()
        {
            MeshNetWorkGraph graph = new();

            graph.AddConnection("Gateway", "Sensor A", 5);

            List<string> traversal =
                graph.BreadthFirstSearch("Gateway");

            Assert.Contains("Sensor A", traversal);
        }

        [Fact]
        public void BreadthFirstSearch_ShouldVisitAllReachableDevices()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            List<string> result =
                graph.BreadthFirstSearch("Gateway");

            Assert.Equal(4, result.Count);
            Assert.Contains("Gateway", result);
            Assert.Contains("Sensor A", result);
            Assert.Contains("Sensor B", result);
            Assert.Contains("Sensor C", result);
        }

        [Fact]
        public void BreadthFirstSearch_ShouldExcludeDisconnectedDevices()
        {
            MeshNetWorkGraph graph = CreateTestGraph();
            graph.AddDevice("Disconnected");

            List<string> result =
                graph.BreadthFirstSearch("Gateway");

            Assert.Equal(4, result.Count);
            Assert.DoesNotContain("Disconnected", result);
        }

        [Fact]
        public void DepthFirstSearch_ShouldVisitAllReachableDevices()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            List<string> result =
                graph.DepthFirstSearch("Gateway");

            Assert.Equal(4, result.Count);
            Assert.Contains("Gateway", result);
            Assert.Contains("Sensor A", result);
            Assert.Contains("Sensor B", result);
            Assert.Contains("Sensor C", result);
        }

        [Fact]
        public void DepthFirstSearch_ShouldExcludeDisconnectedDevices()
        {
            MeshNetWorkGraph graph = CreateTestGraph();
            graph.AddDevice("Disconnected");

            List<string> result =
                graph.DepthFirstSearch("Gateway");

            Assert.Equal(4, result.Count);
            Assert.DoesNotContain("Disconnected", result);
        }

        [Fact]
        public void MinimumSpanningTree_ShouldReturnMinimumTotalWeight()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            var result =
                graph.MinimumSpanningTree("Gateway");

            double totalWeight =
                result.Sum(edge => edge.Weight);

            Assert.Equal(3, result.Count);
            Assert.Equal(10, totalWeight);
        }

        [Fact]
        public void MinimumSpanningTree_ShouldExcludeDisconnectedDevices()
        {
            MeshNetWorkGraph graph = CreateTestGraph();
            graph.AddDevice("Disconnected");

            var result =
                graph.MinimumSpanningTree("Gateway");

            Assert.Equal(3, result.Count);

            Assert.DoesNotContain(
                result,
                edge => edge.DeviceA == "Disconnected" ||
                        edge.DeviceB == "Disconnected");
        }

        [Fact]
        public void FindShortestPath_ShouldReturnCheapestRoute()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            var result = graph.FindShortestPath(
                "Sensor B",
                "Gateway");

            Assert.Equal(7, result.TotalWeight);

            Assert.Equal(
                new List<string>
                {
                    "Sensor B",
                    "Sensor A",
                    "Gateway"
                },
                result.Path);
        }

        [Fact]
        public void FindShortestPath_ShouldReturnEmptyPathWhenDestinationIsUnreachable()
        {
            MeshNetWorkGraph graph = CreateTestGraph();
            graph.AddDevice("Disconnected");

            var result = graph.FindShortestPath(
                "Gateway",
                "Disconnected");

            Assert.Empty(result.Path);
            Assert.Equal(
                double.PositiveInfinity,
                result.TotalWeight);
        }

        [Fact]
        public void FindShortestPath_ShouldReturnZeroWeightWhenStartEqualsDestination()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            var result = graph.FindShortestPath(
                "Gateway",
                "Gateway");

            Assert.Single(result.Path);
            Assert.Equal("Gateway", result.Path[0]);
            Assert.Equal(0, result.TotalWeight);
        }

        [Fact]
        public void Graph_ShouldTreatDeviceIdentifiersAsCaseInsensitive()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            var result = graph.FindShortestPath(
                "gateway",
                "SENSOR A");

            Assert.Equal(5, result.TotalWeight);
            Assert.Equal(2, result.Path.Count);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void AddConnection_ShouldRejectInvalidWeights(double weight)
        {
            MeshNetWorkGraph graph = new();

            Assert.Throws<ArgumentException>(() =>
                graph.AddConnection("Gateway", "Sensor A", weight));
        }


        // ---------------------------------------------------------
        // INDEPENDENT MINIMUM SPANNING TREE VALIDATION
        // ---------------------------------------------------------

        [Fact]
        public void MinimumSpanningTree_ShouldMatchIndependentKruskalResult()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            var result = graph.MinimumSpanningTree("Gateway");

            // Independently calculate the MST using Kruskal.
            var edges = new List<(string A, string B, double Weight)>
            {
                ("Gateway", "Sensor A", 5),
                ("Gateway", "Sensor B", 8),
                ("Sensor A", "Sensor B", 2),
                ("Sensor A", "Sensor C", 6),
                ("Sensor B", "Sensor C", 3)
            };

            double kruskalWeight = CalculateKruskalWeight(
                new[] { "Gateway", "Sensor A", "Sensor B", "Sensor C" },
                edges);

            double primWeight = result.Sum(edge => edge.Weight);

            Assert.Equal(kruskalWeight, primWeight);
            Assert.Equal(10, primWeight);
        }

        [Fact]
        public void MinimumSpanningTree_ShouldContainExactlyVerticesMinusOneEdges()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            var result = graph.MinimumSpanningTree("Gateway");

            int vertexCount = graph.BreadthFirstSearch("Gateway").Count;

            Assert.Equal(vertexCount - 1, result.Count);
        }

        [Fact]
        public void MinimumSpanningTree_ShouldNotContainCycles()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            var result = graph.MinimumSpanningTree("Gateway");

            var parents = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (string device in graph.BreadthFirstSearch("Gateway"))
            {
                parents[device] = device;
            }

            foreach (var edge in result)
            {
                string rootA = FindRoot(parents, edge.DeviceA);
                string rootB = FindRoot(parents, edge.DeviceB);

                // If both endpoints already belong to the same
                // component, this edge would create a cycle.
                Assert.NotEqual(rootA, rootB);

                parents[rootA] = rootB;
            }
        }

        [Fact]
        public void MinimumSpanningTree_ShouldMatchKruskalOnLargerNetwork()
        {
            MeshNetWorkGraph graph = new();

            const int deviceCount = 30;

            var devices = Enumerable.Range(0, deviceCount)
                .Select(i => $"DEVICE-{i:D2}")
                .ToList();

            var edges = new List<(string A, string B, double Weight)>();

            Random random = new(42);

            // First connect every device through a chain.
            // This guarantees that the graph is connected.
            for (int i = 0; i < deviceCount - 1; i++)
            {
                edges.Add((
                    devices[i],
                    devices[i + 1],
                    random.Next(1, 20)));
            }

            // Add additional connections to create alternative routes.
            for (int i = 0; i < 100; i++)
            {
                int a = random.Next(deviceCount);
                int b = random.Next(deviceCount);

                if (a == b)
                    continue;

                string deviceA = devices[a];
                string deviceB = devices[b];

                bool alreadyExists = edges.Any(edge =>
                    (edge.A == deviceA && edge.B == deviceB) ||
                    (edge.A == deviceB && edge.B == deviceA));

                if (!alreadyExists)
                {
                    edges.Add((
                        deviceA,
                        deviceB,
                        random.Next(1, 20)));
                }
            }

            foreach (string device in devices)
            {
                graph.AddDevice(device);
            }

            foreach (var edge in edges)
            {
                graph.AddConnection(
                    edge.A,
                    edge.B,
                    edge.Weight);
            }

            var primResult = graph.MinimumSpanningTree(devices[0]);

            double primWeight = primResult.Sum(edge => edge.Weight);

            double kruskalWeight = CalculateKruskalWeight(
                devices,
                edges);

            Assert.Equal(deviceCount - 1, primResult.Count);
            Assert.Equal(kruskalWeight, primWeight);
        }

        // ---------------------------------------------------------
        // INDEPENDENT KRUSKAL IMPLEMENTATION FOR TESTING
        // ---------------------------------------------------------

        private static double CalculateKruskalWeight(
            IEnumerable<string> devices,
            List<(string A, string B, double Weight)> edges)
        {
            var parents = devices.ToDictionary(
                device => device,
                device => device,
                StringComparer.OrdinalIgnoreCase);

            double totalWeight = 0;
            int selectedEdges = 0;

            foreach (var edge in edges.OrderBy(edge => edge.Weight))
            {
                string rootA = FindRoot(parents, edge.A);
                string rootB = FindRoot(parents, edge.B);

                if (rootA == rootB)
                    continue;

                parents[rootA] = rootB;

                totalWeight += edge.Weight;
                selectedEdges++;
            }

            int expectedEdges = parents.Count - 1;

            if (selectedEdges != expectedEdges)
            {
                throw new InvalidOperationException(
                    "The graph is disconnected and has no spanning tree.");
            }

            return totalWeight;
        }

        private static string FindRoot(
            Dictionary<string, string> parents,
            string device)
        {
            if (!parents.TryGetValue(device, out string? parent))
            {
                throw new InvalidOperationException(
                    $"Unknown device: {device}");
            }

            if (parent == device)
                return device;

            string root = FindRoot(parents, parent);

            parents[device] = root;

            return root;
        }

    
    
        // ---------------------------------------------------------
        // DIJKSTRA SHORTEST-PATH VALIDATION
        // ---------------------------------------------------------

        [Fact]
        public void FindShortestPath_ShouldPreferCheaperMultiHopRouteOverDirectConnection()
        {
            MeshNetWorkGraph graph = new();

            graph.AddConnection("Gateway", "Sensor A", 20);
            graph.AddConnection("Gateway", "Sensor B", 4);
            graph.AddConnection("Sensor B", "Sensor A", 3);

            var result = graph.FindShortestPath(
                "Gateway",
                "Sensor A");

            Assert.Equal(7, result.TotalWeight);

            Assert.Equal(
                new List<string>
                {
                    "Gateway",
                    "Sensor B",
                    "Sensor A"
                },
                result.Path);
        }

        [Fact]
        public void FindShortestPath_ShouldChooseCheapestAmongMultipleAlternativeRoutes()
        {
            MeshNetWorkGraph graph = new();

            graph.AddConnection("Gateway", "Relay A", 5);
            graph.AddConnection("Gateway", "Relay B", 2);
            graph.AddConnection("Relay A", "Relay C", 2);
            graph.AddConnection("Relay B", "Relay C", 3);
            graph.AddConnection("Relay C", "Sensor", 4);
            graph.AddConnection("Relay A", "Sensor", 15);

            var result = graph.FindShortestPath(
                "Gateway",
                "Sensor");

            Assert.Equal(9, result.TotalWeight);

            Assert.Equal(
                new List<string>
                {
                    "Gateway",
                    "Relay B",
                    "Relay C",
                    "Sensor"
                },
                result.Path);
        }

        [Fact]
        public void FindShortestPath_ShouldReturnCorrectCostForLargerNetwork()
        {
            MeshNetWorkGraph graph = new();

            const int deviceCount = 30;

            var devices = Enumerable.Range(0, deviceCount)
                .Select(i => $"DEVICE-{i:D2}")
                .ToList();

            var edges = new List<(string A, string B, double Weight)>();

            Random random = new(42);

            // Guarantee that all devices are connected.
            for (int i = 0; i < deviceCount - 1; i++)
            {
                edges.Add((
                    devices[i],
                    devices[i + 1],
                    random.Next(1, 20)));
            }

            // Add alternative connections.
            for (int i = 0; i < 100; i++)
            {
                int a = random.Next(deviceCount);
                int b = random.Next(deviceCount);

                if (a == b)
                    continue;

                string deviceA = devices[a];
                string deviceB = devices[b];

                bool exists = edges.Any(edge =>
                    (edge.A == deviceA && edge.B == deviceB) ||
                    (edge.A == deviceB && edge.B == deviceA));

                if (!exists)
                {
                    edges.Add((
                        deviceA,
                        deviceB,
                        random.Next(1, 20)));
                }
            }

            foreach (string device in devices)
                graph.AddDevice(device);

            foreach (var edge in edges)
            {
                graph.AddConnection(
                    edge.A,
                    edge.B,
                    edge.Weight);
            }

            var result = graph.FindShortestPath(
                devices[0],
                devices[^1]);

            double expectedWeight = CalculateBellmanFordWeight(
                devices,
                edges,
                devices[0],
                devices[^1]);

            Assert.NotEmpty(result.Path);
            Assert.Equal(expectedWeight, result.TotalWeight);

            Assert.Equal(devices[0], result.Path[0]);
            Assert.Equal(devices[^1], result.Path[^1]);
        }

        [Fact]
        public void FindShortestPath_ShouldReturnValidPathWithMatchingEdgeWeights()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            var result = graph.FindShortestPath(
                "Sensor B",
                "Gateway");

            var knownEdges = new Dictionary<(string, string), double>
            {
                [("Sensor B", "Sensor A")] = 2,
                [("Sensor A", "Gateway")] = 5
            };

            double calculatedWeight = 0;

            for (int i = 0; i < result.Path.Count - 1; i++)
            {
                string current = result.Path[i];
                string next = result.Path[i + 1];

                calculatedWeight += knownEdges[(current, next)];
            }

            Assert.Equal(result.TotalWeight, calculatedWeight);
        }

        [Fact]
        public void FindShortestPath_ShouldRejectUnknownStartingDevice()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            Assert.Throws<ArgumentException>(() =>
                graph.FindShortestPath(
                    "Unknown",
                    "Gateway"));
        }

        [Fact]
        public void FindShortestPath_ShouldRejectUnknownDestinationDevice()
        {
            MeshNetWorkGraph graph = CreateTestGraph();

            Assert.Throws<ArgumentException>(() =>
                graph.FindShortestPath(
                    "Gateway",
                    "Unknown"));
        }

        // ---------------------------------------------------------
        // INDEPENDENT BELLMAN-FORD REFERENCE CALCULATION
        // ---------------------------------------------------------

        private static double CalculateBellmanFordWeight(
            List<string> devices,
            List<(string A, string B, double Weight)> edges,
            string start,
            string destination)
        {
            var distances = devices.ToDictionary(
                device => device,
                device => double.PositiveInfinity,
                StringComparer.OrdinalIgnoreCase);

            distances[start] = 0;

            // Relax every edge until no shorter route is found.
            // Connections are undirected, so both directions are checked.
            for (int i = 0; i < devices.Count - 1; i++)
            {
                bool changed = false;

                foreach (var edge in edges)
                {
                    if (distances[edge.A] + edge.Weight <
                        distances[edge.B])
                    {
                        distances[edge.B] =
                            distances[edge.A] + edge.Weight;

                        changed = true;
                    }

                    if (distances[edge.B] + edge.Weight <
                        distances[edge.A])
                    {
                        distances[edge.A] =
                            distances[edge.B] + edge.Weight;

                        changed = true;
                    }
                }

                if (!changed)
                    break;
            }

            return distances[destination];
        }



    }
}