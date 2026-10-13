
namespace SmartX.Shared.Models;

public class CommandHistoryEntry
{
    public string DeviceMacAddress { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public string Status { get; set; } = string.Empty;
}
