
using SmartX.Shared.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SmartXWPF.Services;

public class SearchHistoryApiService
{
    private readonly ApiService _apiService;

    public SearchHistoryApiService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task RecordSearchAsync(
        string searchArea,
        string searchTerm)
    {
        SearchHistoryEntry entry = new()
        {
            SearchArea = searchArea,
            SearchTerm = searchTerm
        };

        using HttpResponseMessage response =
            await _apiService.Client.PostAsJsonAsync(
                "api/search-history",
                entry);

        response.EnsureSuccessStatusCode();
    }

    public async Task<List<SearchHistoryEntry>> GetHistoryAsync()
    {
        List<SearchHistoryEntry>? history =
            await _apiService.Client
                .GetFromJsonAsync<List<SearchHistoryEntry>>(
                    "api/search-history");

        return history ?? new List<SearchHistoryEntry>();
    }
}
