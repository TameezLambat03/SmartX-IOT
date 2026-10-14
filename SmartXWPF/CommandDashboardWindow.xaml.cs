
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using SmartXWPF.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SmartXWPF;

public partial class CommandDashboardWindow : Window
{
    private readonly SensorApiService _sensorApiService;
    private readonly CommandApiService _commandApiService;
    private readonly AlertApiService _alertApiService;
    private readonly RecommendationApiService _recommendationApiService;

    private List<SensorProfile> _allSensors = new();

    public CommandDashboardWindow()
    {
        InitializeComponent();

        string? apiBaseUrl =
            ConfigurationManager.AppSettings["ApiBaseUrl"];

        ApiService apiService =
            new ApiService(apiBaseUrl!);

        _sensorApiService =
            new SensorApiService(apiService);

        _commandApiService =
            new CommandApiService(apiService);

        _alertApiService =
            new AlertApiService(apiService);

        _recommendationApiService =
            new RecommendationApiService(apiService);

        Loaded += CommandDashboardWindow_Loaded;
    }

    private async void CommandDashboardWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async void BtnRefreshAll_Click(
        object sender,
        RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        BtnUndo.IsEnabled = false;

        try
        {
            bool success =
                await _commandApiService.UndoLastCommandAsync();

            if (success)
            {
                LblCommandStatus.Text =
                    "Last command undone successfully.";

                await RefreshAllAsync();
            }
            else
            {
                LblCommandStatus.Text =
                    "Undo failed. There may be no command to undo.";
            }
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text = $"Undo failed: {ex.Message}";
        }
        finally
        {
            BtnUndo.IsEnabled = true;
        }
    }

    private async Task RefreshAllAsync()
    {
        await RefreshDevicesAsync();
        await RefreshCommandHistoryAsync();
        await RefreshSelectedDeviceStateAsync();
        await RefreshAlertsAsync();
        await RefreshRecommendationsAsync();
    }

    private async Task RefreshDevicesAsync()
    {
        LblCommandStatus.Text =
            "Loading registered devices from the API...";

        try
        {
            var result =
                await _sensorApiService.GetSensorsAsync();

            if (!result.Success)
            {
                _allSensors.Clear();
                LstDevices.ItemsSource = null;
                CmbActuators.ItemsSource = null;

                LblDeviceCount.Text = "—";

                LblCommandStatus.Text =
                    "Could not retrieve devices. Check that the API is running.";

                UpdateCommandButtons();
                return;
            }

            _allSensors = result.Sensors;

            LstDevices.ItemsSource = _allSensors;

            LblDeviceCount.Text =
                _allSensors.Count.ToString();

            CmbActuators.ItemsSource =
                _allSensors
                    .Where(sensor =>
                        sensor.Category == SensorCategory.Actuator)
                    .ToList();

            LblCommandStatus.Text =
                $"Connected. Loaded {_allSensors.Count} registered device(s).";

            UpdateCommandButtons();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load devices: {ex.Message}";

            UpdateCommandButtons();
        }
    }

    private async void CmbActuators_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (CmbActuators.SelectedItem is SensorProfile sensor)
        {
            LblSelectedLocation.Text =
                sensor.DeploymentLocation;

            LblDeviceState.Text = "Loading...";

            LblDeviceState.Foreground =
                System.Windows.Media.Brushes.Orange;

            UpdateCommandButtons();

            await RefreshSelectedDeviceStateAsync();
        }
        else
        {
            LblSelectedLocation.Text = "No actuator selected";
            LblDeviceState.Text = "Unknown";

            UpdateCommandButtons();
        }
    }

    private async void BtnTurnOn_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SendCommandAsync("ON");
    }

    private async void BtnTurnOff_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SendCommandAsync("OFF");
    }

    private async Task SendCommandAsync(string command)
    {
        if (CmbActuators.SelectedItem is not SensorProfile sensor)
        {
            LblCommandStatus.Text =
                "Select an actuator before sending a command.";

            return;
        }

        BtnTurnOn.IsEnabled = false;
        BtnTurnOff.IsEnabled = false;

        LblCommandStatus.Text =
            $"Sending simulated {command} command...";

        try
        {
            CommandRequest request = new()
            {
                DeviceMacAddress = sensor.DeviceMacAddress,
                Command = command
            };

            using HttpResponseMessage response =
                await _commandApiService.SendCommandAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await response.Content.ReadAsStringAsync();

                LblCommandStatus.Text =
                    $"Command failed ({(int)response.StatusCode}): {error}";

                return;
            }

            LblCommandStatus.Text =
                $"Simulated {command} command recorded successfully.";

            await RefreshCommandHistoryAsync();
            await RefreshSelectedDeviceStateAsync();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not send command: {ex.Message}";
        }
        finally
        {
            UpdateCommandButtons();
        }
    }

    private async Task RefreshCommandHistoryAsync()
    {
        try
        {
            var result =
                await _commandApiService.GetCommandHistoryAsync();

            if (!result.Success)
            {
                LblCommandStatus.Text +=
                    " Command history could not be retrieved.";

                return;
            }

            LstCommandHistory.ItemsSource =
                result.History
                    .OrderByDescending(entry => entry.Timestamp)
                    .ToList();

            LblCommandCount.Text =
                result.History.Count.ToString();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load command history: {ex.Message}";
        }
    }

    private async Task RefreshAlertsAsync()
    {
        try
        {
            List<CriticalAlert> alerts =
                await _alertApiService.GetAlertsAsync();

            DgAlerts.ItemsSource = alerts;

            LblActiveAlerts.Text =
                alerts.Count.ToString();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load alerts: {ex.Message}";
        }
    }

    private async Task RefreshRecommendationsAsync()
    {
        try
        {
            List<PredictiveRecommendation> recommendations =
                await _recommendationApiService
                    .GetRecommendationsAsync();

            DgRecommendations.ItemsSource = recommendations;
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load recommendations: {ex.Message}";
        }
    }

    private async Task RefreshSelectedDeviceStateAsync()
    {
        if (CmbActuators.SelectedItem is not SensorProfile sensor)
        {
            LblDeviceState.Text = "Unknown";

            LblDeviceState.Foreground =
                System.Windows.Media.Brushes.Orange;

            return;
        }

        try
        {
            var result =
                await _commandApiService.GetDeviceStateAsync(
                    sensor.DeviceMacAddress);

            if (!result.Success)
            {
                LblDeviceState.Text = "Unavailable";

                LblDeviceState.Foreground =
                    System.Windows.Media.Brushes.Orange;

                return;
            }

            LblDeviceState.Text = result.State;

            LblDeviceState.Foreground =
                result.State == "ON"
                    ? System.Windows.Media.Brushes.LightGreen
                    : result.State == "OFF"
                        ? System.Windows.Media.Brushes.LightCoral
                        : System.Windows.Media.Brushes.Orange;
        }
        catch (Exception)
        {
            LblDeviceState.Text = "Unavailable";

            LblDeviceState.Foreground =
                System.Windows.Media.Brushes.Orange;
        }
    }

    private void UpdateCommandButtons()
    {
        bool actuatorSelected =
            CmbActuators.SelectedItem is SensorProfile;

        BtnTurnOn.IsEnabled = actuatorSelected;
        BtnTurnOff.IsEnabled = actuatorSelected;
    }

    private void BtnMainMenu_Click(
        object sender,
        RoutedEventArgs e)
    {
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
        Close();
    }
}
