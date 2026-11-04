using System;
using System.Windows;
using System.Windows.Controls;
using SmartX.Shared.Models;
using SmartXWPF.Services;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using SmartX.Shared.Enums;
using System.Linq;
using SmartXWPF.DTOs;

namespace SmartXWPF;



public partial class NetworkTopologyView : UserControl
{
    private readonly SensorApiService _sensorApiService;
    private readonly TelemetryApiService _telemetryApiService;
    private readonly MeshNetworkApiService _meshNetworkApiService;
    private List<MeshNetworkNode> _networkNodes = new();
    private readonly NetworkTopologyRenderer _topologyRenderer;
    private List<MeshNetworkConnection> _networkConnections = new();
    private List<MeshNetworkConnection>? _minimumSpanningTree;
    public NetworkTopologyView()
    {
        InitializeComponent();
        string? apiBaseUrl =
    ConfigurationManager.AppSettings["ApiBaseUrl"];
        //constructor initialisations 
        ApiService apiService = new ApiService(apiBaseUrl!);
        _sensorApiService = new SensorApiService(apiService);
        _telemetryApiService = new TelemetryApiService(apiService);
        _meshNetworkApiService = new MeshNetworkApiService(apiService);
      
        _topologyRenderer = new NetworkTopologyRenderer();

        DpStartDate.SelectedDate = DateTime.Today;
        DpEndDate.SelectedDate = DateTime.Today;

        BtnFindRoute.IsEnabled = false;
        BtnQueryHistory.IsEnabled = false;

        Loaded += NetworkTopologyView_Loaded;
    }
    private async void NetworkTopologyView_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        await LoadSensorsAsync();
        await LoadTopologyAsync();
    }

    //load sensors method 
    private async Task LoadSensorsAsync()
    {
        try
        {
            var result = await _sensorApiService.GetSensorsAsync();

            if (!result.Success)
            {
                MessageBox.Show(
                    "Could not retrieve registered sensors from the API.",
                    "Smart-X",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            CmbSensor.ItemsSource = result.Sensors;
            CmbSensor.DisplayMemberPath = nameof(SensorProfile.DeviceMacAddress);
            CmbSensor.SelectedIndex = -1;

            BtnQueryHistory.IsEnabled = result.Sensors.Count > 0;
            BtnFindRoute.IsEnabled = result.Sensors.Count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load sensors: {ex.Message}",
                "Smart-X",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private async void BtnFindRoute_Click(
     object sender,
     RoutedEventArgs e)
    {
        if (CmbSensor.SelectedItem is not SensorProfile sensor)
        {
            LblRouteResult.Text = "Please select a sensor first.";
            return;
        }

        BtnFindRoute.IsEnabled = false;
        LblRouteResult.Text = "Calculating shortest route...";

        try
        {
            MeshNetworkRoute? route =
                await _meshNetworkApiService.GetShortestRouteAsync(
                    sensor.DeviceMacAddress,
                    "GATEWAY-01");

            if (route == null || route.Path.Count == 0)
            {
                LblRouteResult.Text =
                    "No route to the gateway could be found.";

                return;
            }

            string routePath = string.Join(" → ", route.Path);

            LblRouteResult.Text =
                $"Shortest route: {routePath}\n" +
                $"Total route weight: {route.TotalWeight:0.##}";
        }
        catch (Exception ex)
        {
            LblRouteResult.Text =
                $"Could not calculate route: {ex.Message}";
        }
        finally
        {
            BtnFindRoute.IsEnabled = true;
        }
    }

    private async void BtnQueryHistory_Click(
     object sender,
     RoutedEventArgs e)
    {
        if (CmbSensor.SelectedItem is not SensorProfile sensor)
        {
            LblHistoryStatus.Text = "Please select a sensor.";
            return;
        }

        if (DpStartDate.SelectedDate == null ||
            DpEndDate.SelectedDate == null)
        {
            LblHistoryStatus.Text = "Please select both dates.";
            return;
        }

        DateTime start = DpStartDate.SelectedDate.Value.Date;

        DateTime end = DpEndDate.SelectedDate.Value
            .Date.AddDays(1).AddTicks(-1);

        if (start > end)
        {
            LblHistoryStatus.Text =
                "The start date cannot be after the end date.";
            return;
        }

        BtnQueryHistory.IsEnabled = false;
        LblHistoryStatus.Text = "Loading historical telemetry...";

        try
        {
            List<object> rows = new();

            switch (sensor.Category)
            {
                case SensorCategory.Environmental:
                    {
                        var history =
                            await _telemetryApiService.GetEnvironmentalHistoryAsync(
                                start, end, sensor.DeviceMacAddress);

                        rows.AddRange(history.Select(packet => (object)new
                        {
                            packet.Timestamp,
                            packet.DeviceMacAddress,
                            packet.SensorValue,
                            packet.DeploymentLocation
                        }));

                        break;
                    }

                case SensorCategory.PowerConsumption:
                    {
                        var history =
                            await _telemetryApiService.GetPowerHistoryAsync(
                                start, end, sensor.DeviceMacAddress);

                        rows.AddRange(history.Select(packet => (object)new
                        {
                            packet.Timestamp,
                            packet.DeviceMacAddress,
                            packet.SensorValue,
                            packet.DeploymentLocation
                        }));

                        break;
                    }

                case SensorCategory.Actuator:
                    {
                        var history =
                            await _telemetryApiService.GetActuatorHistoryAsync(
                                start, end, sensor.DeviceMacAddress);

                        rows.AddRange(history.Select(packet => (object)new
                        {
                            packet.Timestamp,
                            packet.DeviceMacAddress,
                            packet.SensorValue,
                            packet.DeploymentLocation
                        }));

                        break;
                    }
            }

            DgTelemetryHistory.ItemsSource = rows
                .OrderByDescending(row =>
                    ((dynamic)row).Timestamp)
                .ToList();

            LblHistoryStatus.Text =
                $"Loaded {rows.Count} historical reading(s) for {sensor.DeviceMacAddress}.";
        }
        catch (Exception ex)
        {
            LblHistoryStatus.Text =
                $"Could not load historical telemetry: {ex.Message}";
        }
        finally
        {
            BtnQueryHistory.IsEnabled = true;
        }

    }
    private async Task LoadTopologyAsync()
    {
        try
        {
            MeshNetworkTopology? topology =
                await _meshNetworkApiService.GetTopologyAsync();

            if (topology == null)
            {
                LblHistoryStatus.Text = "Could not retrieve network topology.";
                return;
            }

            // Update the network summary.
            LblTotalNodes.Text = topology.Nodes.Count.ToString();

            // Ask the API for the actual BFS traversal results.
            List<string> reachableDevices =
                await _meshNetworkApiService.GetReachableDevicesAsync();

            // Count nodes that BFS can reach from the gateway.
            HashSet<string> reachableSet =
                new(reachableDevices, StringComparer.OrdinalIgnoreCase);

            int reachableCount = topology.Nodes.Count(
                node => reachableSet.Contains(node.Id));

            // Any topology node not visited by BFS is considered unreachable.
            int offlineCount = topology.Nodes.Count - reachableCount;

            LblReachableNodes.Text = reachableCount.ToString();
            LblOfflineNodes.Text = offlineCount.ToString();

            // We'll use these collections in the next step to draw the graph.
            _networkNodes = topology.Nodes;
            _networkConnections = topology.Connections;
            // Render the retrieved network topology on the WPF canvas.
            _topologyRenderer.Draw(
                NetworkCanvas,
                _networkNodes,
                _networkConnections);
            LblHistoryStatus.Text =
                $"Network loaded: {topology.Nodes.Count} nodes and " +
                $"{topology.Connections.Count} connections.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load network topology: {ex.Message}",
                "Smart-X",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
        private async void CmbTopologyMode_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (_networkNodes.Count == 0)
            return;

        try
        {
            List<MeshNetworkConnection> connections;

            if (CmbTopologyMode.SelectedIndex == 0)
            {
                // Display every connection in the network.
                connections = _networkConnections;
            }
            else
            {
                // Retrieve the MST once, then reuse the result.
                _minimumSpanningTree ??=
                    await _meshNetworkApiService.GetMinimumSpanningTreeAsync();

                connections = _minimumSpanningTree;
            }

            _topologyRenderer.Draw(
                NetworkCanvas,
                _networkNodes,
                connections);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to change topology view: {ex.Message}",
                "Network Topology",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
