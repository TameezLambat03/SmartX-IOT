namespace SmartXWPF.DTOs;

public class MeshNetworkNode
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public bool IsGateway { get; set; }
    public string? Category { get; set; }
    public string? DeploymentLocation { get; set; }
}
