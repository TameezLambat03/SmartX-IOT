using SmartX.Shared.Enums;
namespace SmartX.Shared.Models;

public class SensorProfile
{
    public string DeviceMacAddress { get; set; }
    public string DeploymentLocation { get; set; }
    public SensorCategory Category { get; set; }
    // Attachment information
    public string? AttachmentFileName { get; set; }
    public string? AttachmentFilePath { get; set; }

    public SensorProfile(
        string deviceMacAddress,
        string deploymentLocation,
        SensorCategory category)
    {
        DeviceMacAddress = deviceMacAddress;
        DeploymentLocation = deploymentLocation;
        Category = category;
    }
}