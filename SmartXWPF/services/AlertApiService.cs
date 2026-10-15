using SmartX.Shared.Models;
using System;
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
        using HttpResponseMessage response =
            await _client.GetAsync("api/alerts");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<CriticalAlert>>()
            ?? new List<CriticalAlert>();
    }

    public async Task<List<string>> GetActiveErrorsAsync()
    {
        using HttpResponseMessage response =
            await _client.GetAsync("api/alerts/active");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<string>>()
            ?? new List<string>();
    }
}