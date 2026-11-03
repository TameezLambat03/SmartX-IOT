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
        _sensorApiService = new SensorApiService(apiService);
        _telemetryApiService = new TelemetryApiService(apiService);
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
    private void BtnFindRoute_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBox.Show(
            "Network routing will be connected in the next implementation step.",
            "Smart-X",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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

            // The current demo graph is connected, so every returned node
            // should be reachable from the gateway.
            LblReachableNodes.Text = topology.Nodes.Count.ToString();

            LblOfflineNodes.Text = "0";

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
}