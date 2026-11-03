namespace SmartXAPI.DTOs;

public sealed class MeshNetworkTopology
{
    public string GatewayId { get; init; } = "";

    public List<MeshNetworkNode> Nodes { get; init; } = new();

    public List<MeshNetworkConnection> Connections { get; init; } = new();
}