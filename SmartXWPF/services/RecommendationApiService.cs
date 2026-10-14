
using SmartX.Shared.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SmartXWPF.Services;

public class RecommendationApiService
{
    private readonly HttpClient _client;

    public RecommendationApiService(ApiService apiService)
    {
        _client = apiService.Client;
    }

    public async Task<List<PredictiveRecommendation>>
        GetRecommendationsAsync()
    {
        HttpResponseMessage response =
            await _client.GetAsync("api/recommendations");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<PredictiveRecommendation>>()
            ?? new List<PredictiveRecommendation>();
    }
}
