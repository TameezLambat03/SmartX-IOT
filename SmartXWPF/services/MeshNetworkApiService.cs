using SmartXWPF.DTOs;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SmartXWPF.Services;

public class MeshNetworkApiService
{
    private readonly HttpClient _client;

    public MeshNetworkApiService(ApiService apiService)
    {
        // Reuse the shared HTTP client used by the other API services.
        _client = apiService.Client;
    }

    // Retrieve the complete mesh network topology.
    public async Task<MeshNetworkTopology?> GetTopologyAsync()
    {
        return await _client.GetFromJsonAsync<MeshNetworkTopology>(
            "api/network/topology");
    }

    // Retrieve the minimum spanning tree calculated by Prim's algorithm.
    public async Task<List<MeshNetworkConnection>> GetMinimumSpanningTreeAsync()
    {
        List<MeshNetworkConnection>? connections =
            await _client.GetFromJsonAsync<List<MeshNetworkConnection>>(
                "api/network/mst");

        return connections ?? new List<MeshNetworkConnection>();
    }

    // Calculate the shortest route between two network devices.
    public async Task<MeshNetworkRoute?> GetShortestRouteAsync(
        string startDevice,
        string destinationDevice)
    {
        string url =
            $"api/network/route?startDevice={Uri.EscapeDataString(startDevice)}" +
            $"&destinationDevice={Uri.EscapeDataString(destinationDevice)}";

        return await _client.GetFromJsonAsync<MeshNetworkRoute>(url);
    }
    public async Task<List<string>> GetReachableDevicesAsync()
    {
        List<string>? devices =
            await _client.GetFromJsonAsync<List<string>>(
                "api/network/reachable");

        return devices ?? new List<string>();
    }
}