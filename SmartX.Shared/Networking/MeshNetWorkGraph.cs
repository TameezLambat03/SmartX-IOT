
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
            
            if (string.IsNullOrWhiteSpace(startDevice))
            {
                throw new ArgumentException(
                    "Starting device cannot be empty.",
                    nameof(startDevice));
            }
            
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
        //prims algorithm
        public List<(string DeviceA, string DeviceB, double Weight)> MinimumSpanningTree(//cheapest combo of connections to connec tall devices
    string startDevice)
        {
            // Validate the starting device.
            if (string.IsNullOrWhiteSpace(startDevice))
            {
                throw new ArgumentException(
                    "Starting device cannot be empty.",
                    nameof(startDevice));
            }

            startDevice = startDevice.Trim();

            if (!_adjacencyList.ContainsKey(startDevice))
            {
                throw new ArgumentException(
                    "Starting device does not exist.",
                    nameof(startDevice));
            }

            // Store the connections selected for the minimum spanning tree.
            List<(string DeviceA, string DeviceB, double Weight)> tree = new();

            // Track devices already included in the tree.
            HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);

            // Store candidate connections: source, destination and weight.
            PriorityQueue<(string From, string To, double Weight), double> candidates = new();

            // Begin building the tree at the selected device.
            visited.Add(startDevice);

            // Add connections from the starting device to its neighbours.
            foreach (var neighbour in _adjacencyList[startDevice])
            {
                candidates.Enqueue(
                    (startDevice, neighbour.Key, neighbour.Value),
                    neighbour.Value);
            }

            // Keep selecting the lowest-weight connection available.
            while (candidates.Count > 0)
            {
                var edge = candidates.Dequeue();

                // Skip connections leading to devices already in the tree.
                if (visited.Contains(edge.To))
                {
                    continue;
                }

                // Include the new device and its connection in the tree.
                visited.Add(edge.To);
                tree.Add((edge.From, edge.To, edge.Weight));

                // Add candidate connections from the newly included device.
                foreach (var neighbour in _adjacencyList[edge.To])
                {
                    if (!visited.Contains(neighbour.Key))
                    {
                        candidates.Enqueue(
                            (edge.To, neighbour.Key, neighbour.Value),
                            neighbour.Value);
                    }
                }
            }

            // Return the minimum spanning tree for the starting device's component.
            return tree;
        }

        //Dijkstra's algorithm
        public (List<string> Path, double TotalWeight) FindShortestPath(//What is the cheapest route from this particular sensor to the gateway?
            string startDevice,
            string destinationDevice)
        {
            // Validate the starting device.
            if (string.IsNullOrWhiteSpace(startDevice))
            {
                throw new ArgumentException(
                    "Starting device cannot be empty.",
                    nameof(startDevice));
            }

            // Validate the destination device.
            if (string.IsNullOrWhiteSpace(destinationDevice))
            {
                throw new ArgumentException(
                    "Destination device cannot be empty.",
                    nameof(destinationDevice));
            }

            startDevice = startDevice.Trim();
            destinationDevice = destinationDevice.Trim();

            // Check that both devices exist in the graph.
            if (!_adjacencyList.ContainsKey(startDevice))
                throw new ArgumentException("Starting device does not exist.");

            if (!_adjacencyList.ContainsKey(destinationDevice))
                throw new ArgumentException("Destination device does not exist.");

            // Store the cheapest known cost to reach each device.
            Dictionary<string, double> distances =
                new(StringComparer.OrdinalIgnoreCase);

            // Store the previous device in each cheapest route.
            Dictionary<string, string?> previous =
                new(StringComparer.OrdinalIgnoreCase);

            // Priority queue processes the device with the lowest known cost first.
            PriorityQueue<string, double> queue = new();

            // Initially, every device is considered unreachable.
            foreach (string device in _adjacencyList.Keys)
            {
                distances[device] = double.PositiveInfinity;
                previous[device] = null;
            }

            // The starting device has a cost of zero to reach itself.
            distances[startDevice] = 0;

            queue.Enqueue(startDevice, 0);

            // Continue while there are devices waiting to be explored.
            while (queue.Count > 0)
            {
                if (!queue.TryDequeue(out string currentDevice, out double queuedDistance))
                {
                    continue;
                }

                // Ignore outdated entries where a cheaper route has
                // already been discovered.
                if (queuedDistance > distances[currentDevice])
                {
                    continue;
                }

                // Stop if we have reached the destination.
                if (currentDevice.Equals(
                    destinationDevice,
                    StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // Examine every neighbour of the current device.
                foreach (var neighbour in _adjacencyList[currentDevice])
                {
                    double newDistance =
                        distances[currentDevice] + neighbour.Value;

                    // Check whether this route is cheaper than the known route.
                    if (newDistance < distances[neighbour.Key])
                    {
                        distances[neighbour.Key] = newDistance;
                        previous[neighbour.Key] = currentDevice;

                        // Add the updated route to the priority queue.
                        queue.Enqueue(neighbour.Key, newDistance);
                    }
                }
            }

            // Return an empty path if the destination cannot be reached.
            if (double.IsPositiveInfinity(distances[destinationDevice]))
            {
                return (new List<string>(), double.PositiveInfinity);
            }

            // Reconstruct the route by following the previous-device references.
            List<string> path = new();
            string? current = destinationDevice;

            while (current != null)
            {
                path.Add(current);
                current = previous[current];
            }

            // Reverse the path so it runs from start to destination.
            path.Reverse();

            return (path, distances[destinationDevice]);
        }

    }

}
