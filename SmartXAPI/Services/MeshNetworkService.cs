using SmartXAPI.DTOs;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using SmartX.Shared.Networking;

namespace SmartXAPI.Services;

public class MeshNetworkService
{
    // Unique identifier used for the central gateway in the mesh network.
    public const string GatewayId = "GATEWAY-01";

   
    private readonly MeshNetWorkGraph _graph = new();

    // Stores device information separately so the API can return display-friendly data.
    private readonly List<MeshNetworkNode> _nodes = new();

    
    private readonly List<MeshNetworkConnection> _connections = new();

    // Prevents the demo network from being configured more than once.
    private bool _isConfigured;

    public void ConfigureDemoNetwork(IEnumerable<SensorProfile> sensors)
    {
        // Avoid adding duplicate devices and connections if configuration is called again.
        if (_isConfigured)
            return;

        // locate demo sensors
        SensorProfile? environmentalSensor = sensors.FirstOrDefault(
            sensor => sensor.Category == SensorCategory.Environmental);

        SensorProfile? powerSensor = sensors.FirstOrDefault(
            sensor => sensor.Category == SensorCategory.PowerConsumption);

        SensorProfile? actuatorSensor = sensors.FirstOrDefault(
            sensor => sensor.Category == SensorCategory.Actuator);

        // double check sensors were called before building
        if (environmentalSensor == null ||
            powerSensor == null ||
            actuatorSensor == null)
        {
            throw new InvalidOperationException(
                "The demo mesh network requires an environmental, power, and actuator sensor.");
        }

        // Add the central gateway as a graph device.
        _graph.AddDevice(GatewayId);

        // Add the gateway to the API's node collection for frontend display.
        _nodes.Add(new MeshNetworkNode
        {
            Id = GatewayId,
            Label = "Central Gateway",
            IsGateway = true
        });

        // Register each sensor as a graph device and an API node.
        AddSensorNode(environmentalSensor, "Environmental Sensor");
        AddSensorNode(powerSensor, "Power Consumption Sensor");
        AddSensorNode(actuatorSensor, "Actuator Sensor");
        // Add a disconnected test device to demonstrate BFS reachability.
        const string disconnectedDeviceId = "TEST-UNREACHABLE-01";
        // Register it in the graph, but deliberately do not create any connections.
        _graph.AddDevice(disconnectedDeviceId);
        // Add it to the topology so WPF can display and count it.
        _nodes.Add(new MeshNetworkNode
        {
            Id = disconnectedDeviceId,
            Label = "Disconnected Test Node",
            IsGateway = false,
            Category = "Test",
            DeploymentLocation = "Simulation"
        });



        // Create simulated weighted connections between the gateway and sensors.
        AddConnection(GatewayId, environmentalSensor.DeviceMacAddress, 5);
        AddConnection(GatewayId, powerSensor.DeviceMacAddress, 8);

        // Connect the sensors to one another to form a mesh rather than a simple star.
        AddConnection(
            environmentalSensor.DeviceMacAddress,
            powerSensor.DeviceMacAddress,
            2);

        AddConnection(
            environmentalSensor.DeviceMacAddress,
            actuatorSensor.DeviceMacAddress,
            6);

        AddConnection(
            powerSensor.DeviceMacAddress,
            actuatorSensor.DeviceMacAddress,
            3);

        // Mark the network as configured so it cannot accidentally be duplicated.
        _isConfigured = true;
    }

    private void AddSensorNode(SensorProfile sensor, string label)
    {
        // Add the sensor to the graph so traversal and routing algorithms can use it.
        _graph.AddDevice(sensor.DeviceMacAddress);

        // Store sensor metadata for the API response and WPF dashboard.
        _nodes.Add(new MeshNetworkNode
        {
            Id = sensor.DeviceMacAddress,
            Label = label,
            IsGateway = false,
            Category = sensor.Category.ToString(),
            DeploymentLocation = sensor.DeploymentLocation
        });
    }

    private void AddConnection(string deviceA, string deviceB, double weight)
    {
        // Add the weighted connection to the graph; connections are bidirectional.
        _graph.AddConnection(deviceA, deviceB, weight);

        // Store the same connection as a DTO so the frontend can draw it.
        _connections.Add(new MeshNetworkConnection
        {
            DeviceA = deviceA,
            DeviceB = deviceB,
            Weight = weight
        });
    }

    public MeshNetworkTopology GetTopology()
    {
        // Return the gateway, all registered nodes, and all configured connections.
        return new MeshNetworkTopology
        {
            GatewayId = GatewayId,
            Nodes = _nodes.ToList(),
            Connections = _connections.ToList()
        };
    }

    public List<MeshNetworkConnection> GetMinimumSpanningTree()
    {
        // Run Prim's algorithm starting from the gateway to find the minimum spanning tree.
        var spanningTree = _graph.MinimumSpanningTree(GatewayId);

        // Convert the graph's connection tuples into DTOs for the API response.
        return spanningTree.Select(connection => new MeshNetworkConnection
        {
            DeviceA = connection.DeviceA,
            DeviceB = connection.DeviceB,
            Weight = connection.Weight
        }).ToList();
    }

    public (List<string> Path, double TotalWeight) FindShortestPath(
        string startDevice,
        string destinationDevice)
    {
        // Use Dijkstra's algorithm to find the lowest-cost route between two devices.
        return _graph.FindShortestPath(startDevice, destinationDevice);
    }

    public List<string> GetBreadthFirstTraversal(string startDevice)
    {
        // Explore the network level by level from the selected starting device.
        return _graph.BreadthFirstSearch(startDevice);
    }

    public List<string> GetDepthFirstTraversal(string startDevice)
    {
        // Explore the network by following each branch as deeply as possible.
        return _graph.DepthFirstSearch(startDevice);
    }
}