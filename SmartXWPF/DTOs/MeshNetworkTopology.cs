namespace SmartXWPF.DTOs;

public class MeshNetworkTopology
{
    public string GatewayId { get; set; } = "";

    public List<MeshNetworkNode> Nodes { get; set; } = new();

    public List<MeshNetworkConnection> Connections { get; set; } = new();
}
