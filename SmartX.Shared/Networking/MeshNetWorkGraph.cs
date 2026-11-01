
using System; // Provides exceptions and basic system types.
using System.Collections.Generic; // Provides Dictionary collections.

namespace SmartX.Shared.Networking
{
    public class MeshNetWorkGraph
    {
        // Maps each device to its neighbours and the cost of connecting to them.
        private readonly Dictionary<string, Dictionary<string, double>> _adjacencyList =
            new(StringComparer.OrdinalIgnoreCase);

        public void AddDevice(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) // Check whether the ID is empty or whitespace.
            {
                throw new ArgumentException(
                    "Device cannot be null or empty.",
                    nameof(deviceId)); // Reject invalid device IDs.
            }

            deviceId = deviceId.Trim();
            if (!_adjacencyList.ContainsKey(deviceId)) // Check whether the device already exists.
            {
                _adjacencyList[deviceId] =
                    new Dictionary<string, double>(
                        StringComparer.OrdinalIgnoreCase); // Create an empty neighbour list.
            }
        }

        public void AddConnection(
            string deviceA,
            string deviceB,
            double weight) // Weight represents the connection cost.
        {
            if (weight < 0 ||
                double.IsNaN(weight) ||
                double.IsInfinity(weight)) // Check that the weight is valid.
            {
                throw new ArgumentException(
                    "Connection weight must be a valid non-negative number.",
                    nameof(weight)); // Reject invalid connection costs.
            }

            AddDevice(deviceA); // Ensure the first device exists.
            AddDevice(deviceB); // Ensure the second device exists.

            deviceA = deviceA.Trim();
            deviceB = deviceB.Trim();

            _adjacencyList[deviceA][deviceB] = weight; // Connect A to B with the given weight.
            _adjacencyList[deviceB][deviceA] = weight; // Connect B to A because the graph is undirected.
        }


        // BFS 
        public List<string> BreadthFirstSearch(string startDevice)
        {
            startDevice = startDevice.Trim(); 

            if (!_adjacencyList.ContainsKey(startDevice)) // Check whether the starting device exists.
            {
                throw new ArgumentException("Starting device does not exist.");
            }

            Queue<string> queue = new(); // queues devices(stores) Follows FIFO.
            HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase); 
            List<string> traversalOrder = new(); // Stores the order in which devices are visited.

            queue.Enqueue(startDevice); // Add the starting device to the queue.
            visited.Add(startDevice); 

            while (queue.Count > 0) // Continue while there are devices waiting.
            {
                string currentDevice = queue.Dequeue(); 
                traversalOrder.Add(currentDevice); // Record the device being visited.

                foreach (string neighbour in _adjacencyList[currentDevice].Keys) // Examine its neighbours.
                {
                    if (visited.Add(neighbour)) // Mark an unvisited neighbour as visited.
                    {
                        queue.Enqueue(neighbour); // Add that neighbour to the queue.
                    }
                }
            }

            return traversalOrder; // Return the order in which devices were explored.
        }

        // DFS 
        public List<string> DepthFirstSearch(string startDevice)
        {
            // Validate the starting device ID.
            if (string.IsNullOrWhiteSpace(startDevice))
            {
                throw new ArgumentException(
                    "Starting device cannot be empty.",
                    nameof(startDevice));
            }

            startDevice = startDevice.Trim();

            // Ensure the starting device exists in the graph.
            if (!_adjacencyList.ContainsKey(startDevice))
            {
                throw new ArgumentException(
                    "Starting device does not exist.",
                    nameof(startDevice));
            }

            Stack<string> stack = new(); // Stores devices waiting to be explored.
            HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase); // Tracks visited devices.
            List<string> traversalOrder = new(); // Records the order devices are visited.

            stack.Push(startDevice); // Begin exploration at the selected device.

            while (stack.Count > 0) // Continue while devices remain to explore.
            {
                string currentDevice = stack.Pop(); // Take the most recently added device.

                if (!visited.Add(currentDevice)) // Skip devices already visited.
                {
                    continue;
                }

                traversalOrder.Add(currentDevice); // Record this device in the traversal.

                foreach (string neighbour in _adjacencyList[currentDevice].Keys) // Examine connected neighbours.
                {
                    if (!visited.Contains(neighbour)) // Ignore neighbours already explored.
                    {
                        stack.Push(neighbour); // Schedule the neighbour for exploration.
                    }
                }
            }

            return traversalOrder; // Return the order in which devices were visited.
        }
    }
}
