namespace SmartXAPI.DTOs;

public sealed class MeshNetworkNode
{
    public string Id { get; init; } = "";

    public string Label { get; init; } = "";

    public bool IsGateway { get; init; }

    public string? Category { get; init; }

    public string? DeploymentLocation { get; init; }
}