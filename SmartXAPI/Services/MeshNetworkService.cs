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



        // Create additional simulated devices to expand the mesh network.
        MeshNetworkNode[] simulatedDevices =
        {
    new()
    {
        Id = "RELAY-01",
        Label = "Mesh Relay 01",
        Category = "Relay",
        DeploymentLocation = "Zone A"
    },
    new()
    {
        Id = "RELAY-02",
        Label = "Mesh Relay 02",
        Category = "Relay",
        DeploymentLocation = "Zone B"
    },
    new()
    {
        Id = "ENV-SIM-02",
        Label = "Environmental Sensor 02",
        Category = "Environmental",
        DeploymentLocation = "Zone A"
    },
    new()
    {
        Id = "POWER-SIM-02",
        Label = "Power Sensor 02",
        Category = "Power",
        DeploymentLocation = "Zone B"
    },
    new()
    {
        Id = "ACTUATOR-SIM-02",
        Label = "Actuator 02",
        Category = "Actuator",
        DeploymentLocation = "Zone B"
    },
    new()
    {
        Id = "RELAY-03",
        Label = "Mesh Relay 03",
        Category = "Relay",
        DeploymentLocation = "Zone C"
    },
    new()
    {
        Id = "ENV-SIM-03",
        Label = "Environmental Sensor 03",
        Category = "Environmental",
        DeploymentLocation = "Zone C"
    },
    new()
    {
        Id = "POWER-SIM-03",
        Label = "Power Sensor 03",
        Category = "Power",
        DeploymentLocation = "Zone C"
    },
    new()
    {
        Id = "ACTUATOR-SIM-03",
        Label = "Actuator 03",
        Category = "Actuator",
        DeploymentLocation = "Zone C"
    },
    new()
    {
        Id = "RELAY-04",
        Label = "Mesh Relay 04",
        Category = "Relay",
        DeploymentLocation = "Zone D"
    }
};

        // Register each simulated device in both the graph and API topology.
        foreach (MeshNetworkNode device in simulatedDevices)
        {
            _graph.AddDevice(device.Id);
            _nodes.Add(device);
        }

        // Preserve the original connections.
        AddConnection(GatewayId, environmentalSensor.DeviceMacAddress, 5);
        AddConnection(GatewayId, powerSensor.DeviceMacAddress, 8);

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

        // Connect the original sensors to the expanded mesh.
        AddConnection(environmentalSensor.DeviceMacAddress, "RELAY-01", 4);
        AddConnection(powerSensor.DeviceMacAddress, "RELAY-02", 3);
        AddConnection(actuatorSensor.DeviceMacAddress, "RELAY-03", 5);

        // Gateway connections.
        AddConnection(GatewayId, "RELAY-01", 3);
        AddConnection(GatewayId, "RELAY-02", 7);

        // Zone A connections.
        AddConnection("RELAY-01", "ENV-SIM-02", 2);
        AddConnection("RELAY-01", "POWER-SIM-02", 5);

        // Zone B connections.
        AddConnection("RELAY-02", "POWER-SIM-02", 2);
        AddConnection("RELAY-02", "ACTUATOR-SIM-02", 4);

        // Zone C connections.
        AddConnection("ENV-SIM-02", "RELAY-03", 3);
        AddConnection("RELAY-03", "ACTUATOR-SIM-03", 2);
        AddConnection("POWER-SIM-02", "ACTUATOR-SIM-03", 6);

        // Zone D connections.
        AddConnection("RELAY-03", "RELAY-04", 4);
        AddConnection("RELAY-04", "ENV-SIM-03", 2);
        AddConnection("RELAY-04", "POWER-SIM-03", 3);
        AddConnection("ACTUATOR-SIM-02", "POWER-SIM-03", 5);
        AddConnection("ACTUATOR-SIM-03", "ENV-SIM-03", 4);

        
        // Mark the network as configured.
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

    public void SetDeviceOnline(string deviceId, bool isOnline)
    {
        // Update the graph's device availability.
        _graph.SetDeviceOnline(deviceId, isOnline);

        // Update the corresponding node returned by the topology API.
        MeshNetworkNode? node = _nodes.FirstOrDefault(
            n => n.Id.Equals(
                deviceId,
                StringComparison.OrdinalIgnoreCase));

        if (node != null)
        {
            node.IsOnline = isOnline;
        }
    }

    // Checks whether a device is currently online.
    public bool IsDeviceOnline(string deviceId)
    {
        return _graph.IsDeviceOnline(deviceId);
    }

    // Returns devices that are explicitly offline.
    public List<string> GetOfflineDevices()
    {
        return _graph.GetOfflineDevices();
    }

    // Returns online devices that cannot currently be reached from the gateway.

    public List<string> GetUnreachableDevices()
    {
        return _graph.GetUnreachableDevices(GatewayId);
    }

}