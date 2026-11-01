
using SmartX.Shared.Networking;
using Xunit;
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
            // Arrange
            MeshNetWorkGraph graph = new();
            graph.AddDevice("Gateway");
            graph.AddDevice("Sensor A");

            // Act
            graph.AddConnection("Gateway", "Sensor A", 5);

            // Assert
            List<string> traversal =
                graph.BreadthFirstSearch("Gateway");

            Assert.Contains("Sensor A", traversal);
        }

        [Fact]
        public void BreadthFirstSearch_ShouldVisitAllReachableDevices()
        {
            // Arrange
            MeshNetWorkGraph graph = CreateTestGraph();

            // Act
            List<string> result =
                graph.BreadthFirstSearch("Gateway");

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Contains("Gateway", result);
            Assert.Contains("Sensor A", result);
            Assert.Contains("Sensor B", result);
            Assert.Contains("Sensor C", result);
        }

        [Fact]
        public void DepthFirstSearch_ShouldVisitAllReachableDevices()
        {
            // Arrange
            MeshNetWorkGraph graph = CreateTestGraph();

            // Act
            List<string> result =
                graph.DepthFirstSearch("Gateway");

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Contains("Gateway", result);
            Assert.Contains("Sensor A", result);
            Assert.Contains("Sensor B", result);
            Assert.Contains("Sensor C", result);
        }

        [Fact]
        public void MinimumSpanningTree_ShouldReturnMinimumTotalWeight()
        {
            // Arrange
            MeshNetWorkGraph graph = CreateTestGraph();

            // Act
            var result =
                graph.MinimumSpanningTree("Gateway");

            double totalWeight =
                result.Sum(edge => edge.Weight);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(10, totalWeight);
        }
    }
}
