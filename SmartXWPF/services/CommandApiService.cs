
using SmartX.Shared.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartXWPF.Services;

public class CommandApiService
{
    private readonly HttpClient _client;

    public CommandApiService(ApiService apiService)
    {
        _client = apiService.Client;
    }

    public async Task<HttpResponseMessage> SendCommandAsync(
        CommandRequest request)
    {
        return await _client.PostAsJsonAsync(
            "api/commands",
            request);
    }

    public async Task<bool> UndoLastCommandAsync()
    {
        using HttpResponseMessage response =
            await _client.PostAsync(
                "api/commands/undo",
                null);

        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, List<CommandHistoryEntry> History)>
        GetCommandHistoryAsync()
    {
        HttpResponseMessage response =
            await _client.GetAsync("api/commands");

        if (!response.IsSuccessStatusCode)
        {
            return (false, new List<CommandHistoryEntry>());
        }

        List<CommandHistoryEntry>? history =
            await response.Content
                .ReadFromJsonAsync<List<CommandHistoryEntry>>();

        return (true, history ?? new List<CommandHistoryEntry>());
    }

    public async Task<(bool Success, string State)> GetDeviceStateAsync(
        string macAddress)
    {
        string encodedMacAddress =
            System.Uri.EscapeDataString(macAddress);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"api/commands/{encodedMacAddress}/state");

        if (!response.IsSuccessStatusCode)
        {
            return (false, "Unavailable");
        }

        using JsonDocument document =
            await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

        string? state = document.RootElement
            .GetProperty("state")
            .GetString();

        return (true, state ?? "Unknown");
    }
}
