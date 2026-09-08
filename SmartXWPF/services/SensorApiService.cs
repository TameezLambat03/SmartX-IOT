using SmartX.Shared.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SmartXWPF.Services;

public class SensorApiService
{
    private readonly HttpClient _client;

    public SensorApiService(ApiService apiService)
    {
        _client = apiService.Client;
    }

    public async Task<(bool Success, List<SensorProfile> Sensors)> GetSensorsAsync()
    {
        HttpResponseMessage response =
            await _client.GetAsync("api/sensors");

        if (!response.IsSuccessStatusCode)
        {
            return (false, new List<SensorProfile>());
        }

        List<SensorProfile>? sensors =
            await response.Content.ReadFromJsonAsync<List<SensorProfile>>();

        return (true, sensors ?? new List<SensorProfile>());
    }

    public async Task<HttpResponseMessage> RegisterSensorAsync(
        SensorProfile sensor)
    {
        return await _client.PostAsJsonAsync(
            "api/sensors",
            sensor);
    }
}