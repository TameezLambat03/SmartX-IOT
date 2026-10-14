
namespace SmartX.Shared.Models;

public class PredictiveRecommendation
{
    public string DeviceMacAddress { get; set; } = string.Empty;
    public string DeploymentLocation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public float SensorValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}
