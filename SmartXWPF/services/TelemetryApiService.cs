using SmartX.Shared.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SmartXWPF.Services;

public class TelemetryApiService
{
    private readonly HttpClient _client;

    public TelemetryApiService(ApiService apiService)
    {
        _client = apiService.Client;
    }

    public async Task<HttpResponseMessage> SendEnvironmentalAsync(
        TelemetryPacket<float> packet)
    {
        return await _client.PostAsJsonAsync(
            "api/telemetry/environmental",
            packet);
    }

    public async Task<HttpResponseMessage> SendPowerAsync(
        TelemetryPacket<int> packet)
    {
        return await _client.PostAsJsonAsync(
            "api/telemetry/power",
            packet);
    }

    public async Task<HttpResponseMessage> SendActuatorAsync(
        TelemetryPacket<bool> packet)
    {
        return await _client.PostAsJsonAsync(
            "api/telemetry/actuator",
            packet);
    }

    public async Task<List<float>> GetEnvironmentalAsync()
    {
        List<float>? data =
            await _client.GetFromJsonAsync<List<float>>(
                "api/telemetry/environmental");

        return data ?? new List<float>();
    }
    public async Task<List<int>> GetPowerAsync()
    {
        List<int>? data =
            await _client.GetFromJsonAsync<List<int>>(
                "api/telemetry/power");

        return data ?? new List<int>();
    }

    public async Task<List<bool>> GetActuatorAsync()
    {
        List<bool>? data =
            await _client.GetFromJsonAsync<List<bool>>(
                "api/telemetry/actuator");

        return data ?? new List<bool>();
    }



public async Task<List<TelemetryPacket<float>>>
    GetEnvironmentalHistoryAsync()
    {
        List<TelemetryPacket<float>>? history =
            await _client.GetFromJsonAsync<
                List<TelemetryPacket<float>>>(
                    "api/telemetry/environmental/history");

        return history ?? new List<TelemetryPacket<float>>();
    }
}