
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
using System.Windows.Threading;

namespace SmartXWPF;

public partial class CommandDashboardWindow : Window
{
    private readonly SensorApiService _sensorApiService;
    private readonly CommandApiService _commandApiService;
    private readonly AlertApiService _alertApiService;
    private readonly RecommendationApiService _recommendationApiService;
    private readonly TelemetryApiService _telemetryApiService;
    private readonly SearchHistoryApiService _searchHistoryApiService;
    private readonly DispatcherTimer _searchDebounceTimer;

    private string _pendingSearchArea = string.Empty;
    private string _pendingSearchTerm = string.Empty;

    private List<SensorProfile> _allSensors = new();
    private List<CommandHistoryEntry> _allCommandHistory = new();

    private HashSet<string> _activeErrorMacAddresses =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _activeErrorsLoaded;

    public CommandDashboardWindow()
    {
        InitializeComponent();

        string? apiBaseUrl =
            ConfigurationManager.AppSettings["ApiBaseUrl"];

        ApiService apiService = new ApiService(apiBaseUrl!);

        _sensorApiService = new SensorApiService(apiService);
        _commandApiService = new CommandApiService(apiService);
        _alertApiService = new AlertApiService(apiService);
        _recommendationApiService =
            new RecommendationApiService(apiService);
        _telemetryApiService = new TelemetryApiService(apiService);
        _searchHistoryApiService =
            new SearchHistoryApiService(apiService);

        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };

        _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;

        Loaded += CommandDashboardWindow_Loaded;
        Closed += (_, _) => _searchDebounceTimer.Stop();
    }

    private async void CommandDashboardWindow_Loaded(
        object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async void BtnRefreshAll_Click(
        object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async void BtnUndo_Click(
        object sender, RoutedEventArgs e)
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
        await RefreshActiveErrorsAsync();
        await RefreshCommandHistoryAsync();
        await RefreshSelectedDeviceStateAsync();
        await RefreshAlertsAsync();
        await RefreshRecommendationsAsync();
        await RefreshEnvironmentalHistoryAsync();
        await RefreshSearchHistoryAsync();
    }

    private async Task RefreshDevicesAsync()
    {
        LblCommandStatus.Text =
            "Loading registered devices from the API...";

        try
        {
            var result = await _sensorApiService.GetSensorsAsync();

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
            LblDeviceCount.Text = _allSensors.Count.ToString();

            CmbActuators.ItemsSource = _allSensors
                .Where(sensor =>
                    sensor.Category == SensorCategory.Actuator)
                .ToList();

            ApplyDeviceFilters();
            UpdateCommandButtons();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load devices: {ex.Message}";

            UpdateCommandButtons();
        }
    }

    private async Task RefreshActiveErrorsAsync()
    {
        try
        {
            List<string> activeErrors =
                await _alertApiService.GetActiveErrorsAsync();

            _activeErrorMacAddresses = new HashSet<string>(
                activeErrors,
                StringComparer.OrdinalIgnoreCase);

            _activeErrorsLoaded = true;
            ApplyDeviceFilters();
        }
        catch (Exception)
        {
            _activeErrorMacAddresses.Clear();
            _activeErrorsLoaded = false;
            ApplyDeviceFilters();

            LblCommandStatus.Text =
                "Could not retrieve active errors. Check the API connection.";
        }
    }

    private void TxtDeviceSearch_TextChanged(
        object sender, TextChangedEventArgs e)
    {
        ApplyDeviceFilters();

        if (IsLoaded)
        {
            ScheduleSearchRecord("Devices", TxtDeviceSearch.Text);
        }
    }

    private void CmbCategoryFilter_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        ApplyDeviceFilters();
    }

    private void ChkActiveAlertsOnly_Changed(
        object sender, RoutedEventArgs e)
    {
        ApplyDeviceFilters();
    }

    private void ApplyDeviceFilters()
    {
        if (!IsLoaded)
            return;

        string searchText = TxtDeviceSearch.Text?.Trim() ?? string.Empty;

        string selectedCategory =
            (CmbCategoryFilter.SelectedItem as ComboBoxItem)
                ?.Content?.ToString() ?? "All Categories";

        bool activeAlertsOnly = ChkActiveAlertsOnly.IsChecked == true;

        if (activeAlertsOnly && !_activeErrorsLoaded)
        {
            LstDevices.ItemsSource = new List<SensorProfile>();

            LblCommandStatus.Text =
                "Active-error data is unavailable. Check the API and refresh.";

            return;
        }

        IEnumerable<SensorProfile> filteredSensors = _allSensors;

        if (selectedCategory != "All Categories")
        {
            filteredSensors = filteredSensors.Where(sensor =>
                sensor.Category.ToString().Equals(
                    selectedCategory,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (activeAlertsOnly)
        {
            filteredSensors = filteredSensors.Where(sensor =>
                _activeErrorMacAddresses.Contains(sensor.DeviceMacAddress));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filteredSensors = filteredSensors.Where(sensor =>
                (sensor.DeviceMacAddress ?? string.Empty).Contains(
                    searchText, StringComparison.OrdinalIgnoreCase)
                ||
                (sensor.DeploymentLocation ?? string.Empty).Contains(
                    searchText, StringComparison.OrdinalIgnoreCase));
        }

        List<SensorProfile> results = filteredSensors.ToList();
        LstDevices.ItemsSource = results;

        LblCommandStatus.Text = activeAlertsOnly
            ? $"Showing {results.Count} device(s) matching the active-alert filters."
            : $"Showing {results.Count} of {_allSensors.Count} registered device(s).";
    }

    private async void CmbActuators_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (CmbActuators.SelectedItem is SensorProfile sensor)
        {
            LblSelectedLocation.Text = sensor.DeploymentLocation;
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
        object sender, RoutedEventArgs e)
    {
        await SendCommandAsync("ON");
    }

    private async void BtnTurnOff_Click(
        object sender, RoutedEventArgs e)
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

            _allCommandHistory = result.History
                .OrderByDescending(entry => entry.Timestamp)
                .ToList();

            ApplyCommandHistoryFilters();
            LblCommandCount.Text = _allCommandHistory.Count.ToString();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load command history: {ex.Message}";
        }
    }

    private void TxtCommandSearch_TextChanged(
        object sender, TextChangedEventArgs e)
    {
        ApplyCommandHistoryFilters();

        if (IsLoaded)
        {
            ScheduleSearchRecord(
                "Command History", TxtCommandSearch.Text);
        }
    }

    private void CmbCommandFilter_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        ApplyCommandHistoryFilters();
    }

    private void ApplyCommandHistoryFilters()
    {
        if (!IsLoaded)
            return;

        string searchText = TxtCommandSearch.Text?.Trim() ?? string.Empty;

        string selectedCommand =
            (CmbCommandFilter.SelectedItem as ComboBoxItem)
                ?.Content?.ToString() ?? "All Commands";

        IEnumerable<CommandHistoryEntry> filteredHistory =
            _allCommandHistory;

        if (selectedCommand != "All Commands")
        {
            filteredHistory = filteredHistory.Where(entry =>
                string.Equals(
                    entry.Command, selectedCommand,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filteredHistory = filteredHistory.Where(entry =>
                (entry.Command ?? string.Empty).Contains(
                    searchText, StringComparison.OrdinalIgnoreCase)
                ||
                (entry.DeviceMacAddress ?? string.Empty).Contains(
                    searchText, StringComparison.OrdinalIgnoreCase)
                ||
                entry.Timestamp.ToString().Contains(
                    searchText, StringComparison.OrdinalIgnoreCase));
        }

        LstCommandHistory.ItemsSource = filteredHistory.ToList();
    }

    private void ScheduleSearchRecord(
        string searchArea, string? searchTerm)
    {
        string term = searchTerm?.Trim() ?? string.Empty;

        _searchDebounceTimer.Stop();

        if (string.IsNullOrWhiteSpace(term))
        {
            _pendingSearchArea = string.Empty;
            _pendingSearchTerm = string.Empty;
            return;
        }

        _pendingSearchArea = searchArea;
        _pendingSearchTerm = term;
        _searchDebounceTimer.Start();
    }

    private async void SearchDebounceTimer_Tick(
        object? sender, EventArgs e)
    {
        _searchDebounceTimer.Stop();

        string area = _pendingSearchArea;
        string term = _pendingSearchTerm;

        if (string.IsNullOrWhiteSpace(area) ||
            string.IsNullOrWhiteSpace(term))
            return;

        try
        {
            await _searchHistoryApiService.RecordSearchAsync(area, term);
            await RefreshSearchHistoryAsync();
            await RefreshRecommendationsAsync();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not record search history: {ex.Message}";
        }
    }

    private async Task RefreshSearchHistoryAsync()
    {
        try
        {
            var history =
                await _searchHistoryApiService.GetHistoryAsync();

            DgSearchHistory.ItemsSource = history
                .OrderByDescending(entry => entry.Timestamp)
                .Select(entry => new
                {
                    entry.SearchArea,
                    entry.SearchTerm,
                    Timestamp = entry.Timestamp.Kind == DateTimeKind.Utc
                        ? entry.Timestamp.ToLocalTime()
                        : entry.Timestamp
                })
                .ToList();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load search history: {ex.Message}";
        }
    }

    private async Task RefreshAlertsAsync()
    {
        try
        {
            List<CriticalAlert> alerts =
                await _alertApiService.GetAlertsAsync();

            DgAlerts.ItemsSource = alerts;

            LblActiveAlerts.Text = _activeErrorsLoaded
                ? _activeErrorMacAddresses.Count.ToString()
                : "—";
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
                await _recommendationApiService.GetRecommendationsAsync();

            DgRecommendations.ItemsSource = recommendations;
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load recommendations: {ex.Message}";
        }
    }

    private async Task RefreshEnvironmentalHistoryAsync()
    {
        try
        {
            List<TelemetryPacket<float>> history =
                await _telemetryApiService.GetEnvironmentalHistoryAsync();

            DgEnvironmentalHistory.ItemsSource = history
                .OrderByDescending(packet => packet.Timestamp)
                .Select(packet => new
                {
                    packet.DeviceMacAddress,
                    packet.DeploymentLocation,
                    Timestamp = packet.Timestamp.Kind == DateTimeKind.Utc
                        ? packet.Timestamp.ToLocalTime()
                        : packet.Timestamp,
                    packet.SensorValue
                })
                .ToList();
        }
        catch (Exception ex)
        {
            LblCommandStatus.Text =
                $"Could not load environmental history: {ex.Message}";
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

            LblDeviceState.Foreground = result.State == "ON"
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
        object sender, RoutedEventArgs e)
    {
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
        Close();
    }
}
