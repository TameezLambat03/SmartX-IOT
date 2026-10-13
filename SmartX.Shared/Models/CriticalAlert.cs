namespace SmartX.Shared.Models;

public class CriticalAlert
{
    public string DeviceMacAddress { get; set; } = string.Empty;

    public string DeploymentLocation { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public float SensorValue { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

}