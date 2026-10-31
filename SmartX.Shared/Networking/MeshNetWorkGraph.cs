using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Shared.Networking
{
    public class MeshNetWorkGraph
    {
        private readonly HashSet<string> _devices = new(StringComparer.OrdinalIgnoreCase);//collection of strings that does not allow dupelicates 
        //map each unqiue device to its neighbours using adjacency list
        private readonly Dictionary<string, HashSet<string>> _adjacencyList = new(StringComparer.OrdinalIgnoreCase);//Dictionary<string, HashSet<string>> maps each device ID to its set of neighbours.
        public void AddDevice(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("Device cannot be null or empty.", nameof(deviceId));//exception for blank device input
            }
            deviceId = deviceId.Trim();//remove white space before and after the string
            if (!_adjacencyList.ContainsKey(deviceId))
            {
                _adjacencyList[deviceId] =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
        public void AddConnection(string deviceA, string deviceB)
        {
            AddDevice(deviceA);
            AddDevice(deviceB);
            //mapping each devie to its neighbour
            _adjacencyList[deviceA].Add(deviceB);
            _adjacencyList[deviceB].Add(deviceA);
        }
    }
    
}
