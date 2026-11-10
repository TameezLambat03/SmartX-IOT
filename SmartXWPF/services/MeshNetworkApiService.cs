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
    // Retrieve devices explored using Breadth-First Search.
public async Task<List<string>> GetReachableDevicesAsync(
    string startDevice = "GATEWAY-01")
{
    string url =
        $"api/network/reachable?startDevice={Uri.EscapeDataString(startDevice)}";

    List<string>? devices =
        await _client.GetFromJsonAsync<List<string>>(url);

    return devices ?? new List<string>();
}

// Retrieve devices explored using Depth-First Search.
public async Task<List<string>> GetDepthFirstTraversalAsync(
    string startDevice)
{
    string url =
        $"api/network/depth-first?startDevice={Uri.EscapeDataString(startDevice)}";

    List<string>? devices =
        await _client.GetFromJsonAsync<List<string>>(url);

    return devices ?? new List<string>();
}

    // Updates whether a network device is online or offline.
    public async Task SetDeviceOnlineAsync(
        string deviceId,
        bool isOnline)
    {
        string url =
            $"api/network/devices/{Uri.EscapeDataString(deviceId)}/status?isOnline={isOnline.ToString().ToLowerInvariant()}";

        using HttpResponseMessage response =
            await _client.PutAsync(url, null);

        response.EnsureSuccessStatusCode();
    }
}
