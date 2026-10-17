
namespace SmartX.Shared.Models;

public class SearchHistoryEntry
{
    public string SearchArea { get; set; } = string.Empty;

    public string SearchTerm { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
