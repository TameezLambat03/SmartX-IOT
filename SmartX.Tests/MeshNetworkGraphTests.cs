
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
    }
}
