using SmartXWPF.Services;
using System;
using System.Configuration;
using System.Windows;

namespace SmartXWPF;

public partial class CommandDashboardWindow : Window
{
    private readonly SensorApiService _sensorApiService;

    public CommandDashboardWindow()
    {
        InitializeComponent();

        // Use the same API URL configuration as the sensor dashboard.
        string? apiBaseUrl =
            ConfigurationManager.AppSettings["ApiBaseUrl"];

        ApiService apiService =
            new ApiService(apiBaseUrl!);

        _sensorApiService =
            new SensorApiService(apiService);

        Loaded += CommandDashboardWindow_Loaded;
    }

    private async void CommandDashboardWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await RefreshDevicesAsync();
    }

    private async void BtnRefreshDevices_Click(
        object sender,
        RoutedEventArgs e)
    {
        await RefreshDevicesAsync();
    }

    private async System.Threading.Tasks.Task RefreshDevicesAsync()
    {
        LblConnectionStatus.Text =
            "Loading registered devices from the Smart-X API...";

        try
        {
            var result =
                await _sensorApiService.GetSensorsAsync();

            if (!result.Success)
            {
                LblDeviceCount.Text = "—";
                LstDevices.ItemsSource = null;

                LblConnectionStatus.Text =
                    "Could not retrieve devices. Check that the Smart-X API is running.";

                return;
            }

            LstDevices.ItemsSource = result.Sensors;

            LblDeviceCount.Text =
                result.Sensors.Count.ToString();

            if (result.Sensors.Count == 0)
            {
                LblConnectionStatus.Text =
                    "Connected to the API, but no devices are registered yet.";
            }
            else
            {
                LblConnectionStatus.Text =
                    $"Connected successfully. Loaded {result.Sensors.Count} registered device(s).";
            }
        }
        catch (Exception)
        {
            LblDeviceCount.Text = "—";
            LstDevices.ItemsSource = null;

            LblConnectionStatus.Text =
                "Could not connect to the Smart-X API. Check the API is running and verify ApiBaseUrl in App.config.";
        }
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