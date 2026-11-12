
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using SmartXWPF.DTOs;
using SmartXWPF.Services;

namespace SmartXWPF;

public partial class NetworkTopologyView : UserControl
{
    private readonly SensorApiService _sensorApiService;
    private readonly TelemetryApiService _telemetryApiService;
    private readonly MeshNetworkApiService _meshNetworkApiService;

    private readonly NetworkTopologyRenderer _topologyRenderer;

    private List<MeshNetworkNode> _networkNodes = new();
    private List<MeshNetworkConnection> _networkConnections = new();
    private List<MeshNetworkConnection>? _minimumSpanningTree;

    public NetworkTopologyView()
    {
        InitializeComponent();

        string? apiBaseUrl =
            ConfigurationManager.AppSettings["ApiBaseUrl"];

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

    // Load actual registered sensors for historical telemetry.
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
            CmbSensor.SelectedIndex = -1;

            BtnQueryHistory.IsEnabled = result.Sensors.Count > 0;
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

    // Load all graph nodes and populate the routing selector.

    private async Task LoadTopologyAsync()
    {
        // Capture the current dashboard state before replacing network nodes.
        string? selectedTraversalId =
            (CmbTraversalStart.SelectedItem as MeshNetworkNode)?.Id;

        string? selectedManagementId =
            (CmbDeviceManagement.SelectedItem as MeshNetworkNode)?.Id;

        string? selectedRouteId =
            (CmbRouteDevice.SelectedItem as MeshNetworkNode)?.Id;

        int selectedAlgorithmIndex =
            CmbTraversalAlgorithm.SelectedIndex;

        int selectedTopologyMode =
            CmbTopologyMode.SelectedIndex;

        string traversalResult = LblTraversalResult.Text;
        string routeResult = LblRouteResult.Text;
        string managementResult = LblDeviceManagementResult.Text;

        try
        {
            MeshNetworkTopology? topology =
                await _meshNetworkApiService.GetTopologyAsync();

            if (topology == null)
            {
                LblHistoryStatus.Text =
                    "Could not retrieve network topology.";
                return;
            }

            LblTotalNodes.Text = topology.Nodes.Count.ToString();

            List<string> reachableDevices =
                await _meshNetworkApiService.GetReachableDevicesAsync();

            List<string> unreachableDevices =
                await _meshNetworkApiService.GetUnreachableDevicesAsync();

            LblUnreachableDevices.Text = unreachableDevices.Count == 0
                ? "No online devices are currently cut off from the gateway."
                : string.Join(
                    Environment.NewLine,
                    unreachableDevices.Select(device => $"• {device}"));

            HashSet<string> reachableSet =
                new(reachableDevices, StringComparer.OrdinalIgnoreCase);

            int reachableCount = topology.Nodes.Count(
                node => reachableSet.Contains(node.Id));

            int offlineCount = topology.Nodes.Count - reachableCount;

            LblReachableNodes.Text = reachableCount.ToString();
            LblOfflineNodes.Text = offlineCount.ToString();

            _networkNodes = topology.Nodes;
            _networkConnections = topology.Connections;

            // Recalculate the network costs.
            double fullMeshCost = _networkConnections.Sum(
                connection => connection.Weight);

            _minimumSpanningTree =
                await _meshNetworkApiService.GetMinimumSpanningTreeAsync();

            double mstCost = _minimumSpanningTree.Sum(
                connection => connection.Weight);

            LblFullMeshCost.Text = fullMeshCost.ToString("F2");
            LblMstCost.Text = mstCost.ToString("F2");
            LblCostSaved.Text =
                (fullMeshCost - mstCost).ToString("F2");

            // Refresh device management choices.
            CmbDeviceManagement.ItemsSource = _networkNodes;

            // Refresh traversal choices.
            CmbTraversalStart.ItemsSource = _networkNodes;

            // Restore the previously selected traversal device.
            MeshNetworkNode? traversalDevice =
                _networkNodes.FirstOrDefault(node =>
                    string.Equals(
                        node.Id,
                        selectedTraversalId,
                        StringComparison.OrdinalIgnoreCase));

            CmbTraversalStart.SelectedItem =
                traversalDevice ??
                _networkNodes.FirstOrDefault(node => node.IsGateway);

            // Restore the device-management selection.
            MeshNetworkNode? managementDevice =
                _networkNodes.FirstOrDefault(node =>
                    string.Equals(
                        node.Id,
                        selectedManagementId,
                        StringComparison.OrdinalIgnoreCase));

            CmbDeviceManagement.SelectedItem = managementDevice;

            // Restore route choices.
            List<MeshNetworkNode> routeDevices = _networkNodes
                .Where(node =>
                    !node.IsGateway &&
                    !string.Equals(
                        node.Category,
                        "Relay",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        node.Category,
                        "Test",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            CmbRouteDevice.ItemsSource = routeDevices;

            MeshNetworkNode? routeDevice =
                routeDevices.FirstOrDefault(node =>
                    string.Equals(
                        node.Id,
                        selectedRouteId,
                        StringComparison.OrdinalIgnoreCase));

            CmbRouteDevice.SelectedItem = routeDevice;

            BtnFindRoute.IsEnabled = routeDevices.Count > 0;

            // Preserve the selected algorithm and topology mode.
            if (selectedAlgorithmIndex >= 0)
                CmbTraversalAlgorithm.SelectedIndex =
                    selectedAlgorithmIndex;

            if (selectedTopologyMode >= 0)
                CmbTopologyMode.SelectedIndex =
                    selectedTopologyMode;

            // Preserve previous results rather than clearing them.
            LblTraversalResult.Text = traversalResult;
            LblRouteResult.Text = routeResult;
            LblDeviceManagementResult.Text = managementResult;

            // Re-render using the currently selected topology mode.
            List<MeshNetworkConnection> displayedConnections =
                CmbTopologyMode.SelectedIndex == 1
                    ? _minimumSpanningTree
                        ?? new List<MeshNetworkConnection>()
                    : _networkConnections;

            _topologyRenderer.Draw(
                NetworkCanvas,
                _networkNodes,
                displayedConnections);

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


    // Calculate a Dijkstra shortest path from any selectable sensor node.
    private async void BtnFindRoute_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CmbRouteDevice.SelectedItem is not MeshNetworkNode device)
        {
            LblRouteResult.Text =
                "Please select a device first.";
            return;
        }

        BtnFindRoute.IsEnabled = false;
        LblRouteResult.Text = "Calculating shortest route...";

        try
        {
            MeshNetworkRoute? route =
                await _meshNetworkApiService.GetShortestRouteAsync(
                    device.Id,
                    "GATEWAY-01");

            if (route == null || route.Path.Count == 0)
            {
                LblRouteResult.Text =
                    "No route to the gateway could be found.";
                return;
            }

            string routePath = string.Join(" → ", route.Path);

            LblRouteResult.Text =
                $"Starting device: {device.Id}\n" +
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
    // Run BFS or DFS from the selected starting device.
    private async void BtnRunTraversal_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CmbTraversalStart.SelectedItem is not MeshNetworkNode startNode)
        {
            LblTraversalResult.Text =
                "Please select a starting device.";
            return;
        }

        if (CmbTraversalAlgorithm.SelectedItem is not ComboBoxItem selectedAlgorithm)
        {
            LblTraversalResult.Text =
                "Please select a traversal algorithm.";
            return;
        }

        bool useBfs = selectedAlgorithm.Content.ToString()!
            .StartsWith("Breadth", StringComparison.OrdinalIgnoreCase);

        BtnRunTraversal.IsEnabled = false;
        LblTraversalResult.Text = "Traversing network...";

        try
        {
            List<string> visitedDevices;

            if (useBfs)
            {
                visitedDevices =
                    await _meshNetworkApiService.GetReachableDevicesAsync(
                        startNode.Id);
            }
            else
            {
                visitedDevices =
                    await _meshNetworkApiService.GetDepthFirstTraversalAsync(
                        startNode.Id);
            }

            string traversalOrder =
                string.Join(" → ", visitedDevices);

            LblTraversalResult.Text =
                $"Algorithm: {(useBfs ? "BFS" : "DFS")}\n" +
                $"Starting device: {startNode.Id}\n" +
                $"Devices visited ({visitedDevices.Count}):\n" +
                traversalOrder;
        }
        catch (Exception ex)
        {
            LblTraversalResult.Text =
                $"Traversal failed: {ex.Message}";
        }
        finally
        {
            BtnRunTraversal.IsEnabled = true;
        }
    }
    // Query historical telemetry for actual registered sensors only.
    private async void BtnQueryHistory_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CmbSensor.SelectedItem is not SensorProfile sensor)
        {
            LblHistoryStatus.Text =
                "Please select a registered sensor.";
            return;
        }

        if (DpStartDate.SelectedDate == null ||
            DpEndDate.SelectedDate == null)
        {
            LblHistoryStatus.Text =
                "Please select both dates.";
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
                                start,
                                end,
                                sensor.DeviceMacAddress);

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
                                start,
                                end,
                                sensor.DeviceMacAddress);

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
                                start,
                                end,
                                sensor.DeviceMacAddress);

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
                $"Loaded {rows.Count} historical reading(s) for " +
                $"{sensor.DeviceMacAddress}.";
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

    // Switch between the full mesh and minimum spanning tree.
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
                connections = _networkConnections;
            }
            else
            {
                connections = _minimumSpanningTree
      ?? new List<MeshNetworkConnection>();
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
    // Display the selected device's current availability.
    private void CmbDeviceManagement_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (CmbDeviceManagement.SelectedItem is not MeshNetworkNode device)
        {
            LblDeviceStatus.Text =
                "Select a device to view its status.";

            BtnToggleDeviceStatus.IsEnabled = false;
            return;
        }

        LblDeviceStatus.Text =
            $"Device status: {(device.IsOnline ? "ONLINE" : "OFFLINE")}";

        BtnToggleDeviceStatus.Content =
            device.IsOnline
                ? "Take Device Offline"
                : "Bring Device Online";

        BtnToggleDeviceStatus.IsEnabled = true;
    }
    // Switches the selected device between online and offline.
    private async void BtnToggleDeviceStatus_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CmbDeviceManagement.SelectedItem is not MeshNetworkNode device)
        {
            return;
        }

        BtnToggleDeviceStatus.IsEnabled = false;
        LblDeviceManagementResult.Text = "Updating device status...";

        try

        {
            string deviceId = device.Id;//save device before making api request
            bool newStatus = !device.IsOnline;

            await _meshNetworkApiService.SetDeviceOnlineAsync(
                device.Id,
                newStatus);

            // Reload the topology to retrieve the updated device status.
            await LoadTopologyAsync();

            // Restore the selected device after refreshing the topology.
            MeshNetworkNode? updatedDevice = _networkNodes.FirstOrDefault(
                node => node.Id.Equals(
                    device.Id,
                    StringComparison.OrdinalIgnoreCase));

            CmbDeviceManagement.SelectedItem = updatedDevice;

            LblDeviceManagementResult.Text =
                $"{device.Id} is now {(newStatus ? "ONLINE" : "OFFLINE")}.";

        }
        catch (Exception ex)
        {
            LblDeviceManagementResult.Text =
                $"Could not update device status: {ex.Message}";
        }
        finally
        {
            BtnToggleDeviceStatus.IsEnabled = true;
        }
    }

}