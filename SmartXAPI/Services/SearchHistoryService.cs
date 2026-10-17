
using SmartX.Shared.Models;

namespace SmartXAPI.Services;

public class SearchHistoryService
{
    private const int MaxEntries = 200;
    private const int RepeatSearchThreshold = 3;
    private static readonly TimeSpan RepeatSearchWindow =
        TimeSpan.FromMinutes(30);

    private readonly List<SearchHistoryEntry> _history = new();
    private readonly object _lock = new();

    private readonly PredictiveRecommendationService _recommendationService;

    public SearchHistoryService(
        PredictiveRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public void RecordSearch(string searchArea, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchArea) ||
            string.IsNullOrWhiteSpace(searchTerm))
        {
            return;
        }

        string area = searchArea.Trim();
        string term = searchTerm.Trim();
        DateTime timestamp = DateTime.UtcNow;

        lock (_lock)
        {
            _history.Add(new SearchHistoryEntry
            {
                SearchArea = area,
                SearchTerm = term,
                Timestamp = timestamp
            });

            if (_history.Count > MaxEntries)
            {
                _history.RemoveAt(0);
            }

            DateTime windowStart = timestamp - RepeatSearchWindow;

            int repeatedCount = _history.Count(entry =>
                entry.SearchArea.Equals(
                    area, StringComparison.OrdinalIgnoreCase) &&
                entry.SearchTerm.Equals(
                    term, StringComparison.OrdinalIgnoreCase) &&
                entry.Timestamp >= windowStart &&
                entry.Timestamp <= timestamp);

            if (repeatedCount >= RepeatSearchThreshold)
            {
                _recommendationService.AnalyseRepeatedSearch(
                    area, term, repeatedCount, timestamp);
            }
        }
    }

    public IReadOnlyList<SearchHistoryEntry> GetHistory()
    {
        lock (_lock)
        {
            return _history
                .OrderByDescending(entry => entry.Timestamp)
                .ToList();
        }
    }
}
