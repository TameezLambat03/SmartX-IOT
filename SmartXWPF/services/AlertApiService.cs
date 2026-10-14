
using SmartX.Shared.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SmartXWPF.Services;

public class AlertApiService
{
    private readonly HttpClient _client;

    public AlertApiService(ApiService apiService)
    {
        _client = apiService.Client;
    }

    public async Task<List<CriticalAlert>> GetAlertsAsync()
    {
        HttpResponseMessage response =
            await _client.GetAsync("api/alerts");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<CriticalAlert>>()
            ?? new List<CriticalAlert>();
    }
}
