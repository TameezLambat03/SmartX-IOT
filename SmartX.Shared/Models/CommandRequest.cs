
namespace SmartX.Shared.Models;

public class CommandRequest
{
    public string DeviceMacAddress { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;
}
