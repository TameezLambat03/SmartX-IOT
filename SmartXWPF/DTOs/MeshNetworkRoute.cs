namespace SmartXWPF.DTOs;

public class MeshNetworkRoute
{
    public string StartDevice { get; set; } = "";
    public string DestinationDevice { get; set; } = "";

    public List<string> Path { get; set; } = new();

    public double TotalWeight { get; set; }
}
