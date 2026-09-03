using Microsoft.Win32;
using SmartX.Shared;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SmartXWPF;



public partial class SensorDashboardWindow : Window
{
    private readonly HttpClient _httpClient = new HttpClient();
    private string _attachedFilePath = string.Empty;




    public SensorDashboardWindow()
    {
        InitializeComponent();

        _httpClient.BaseAddress =
            new Uri(ConfigurationManager.AppSettings["ApiBaseUrl"]);

        Loaded += async (sender, e) =>
        {
            await LoadRegisteredSensorCount();
        };
    }
    private async Task LoadRegisteredSensorCount()
    {
        try
        {
            var response =
                await _httpClient.GetAsync("/api/sensors");

            if (response.IsSuccessStatusCode)
            {
                var sensors =
                    await response.Content
                        .ReadFromJsonAsync<List<SensorProfile>>();

                int count = sensors?.Count ?? 0;
                MessageBox.Show(
    $"API returned {count} sensor(s).",
    "Smart-X Diagnostic");

                LblDeviceCount.Text = count.ToString();

                LogTelemetryFeed(
                    $"[SYSTEM] Loaded {count} registered sensor profile(s).");
            }
        }
        catch (Exception ex)
        {
            LogTelemetryFeed(
                $"[SYSTEM WARNING] Could not load registered sensors: {ex.Message}");
        }
    }

    private void BtnAttachFile_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Config & Log Files (*.json;*.txt;*.log;*.png;*.jpg)|*.json;*.txt;*.log;*.png;*.jpg|All Files (*.*)|*.*",
            Title = "Select Sensor Profile Attachment"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _attachedFilePath = openFileDialog.FileName;
            LblAttachedFile.Text = $"Attached: {System.IO.Path.GetFileName(_attachedFilePath)} ({new FileInfo(_attachedFilePath).Length / 1024} KB)";
            LblAttachedFile.Foreground = new SolidColorBrush(Colors.LightGreen);
            LogTelemetryFeed($"Attached profile artifact: {System.IO.Path.GetFileName(_attachedFilePath)}");
        }
    }
    private async void BtnRegisterSensor_Click(object sender, RoutedEventArgs e)
    {
        string mac = TxtMacAddress.Text.Trim();
        string deployment = TxtDeploymentPath.Text.Trim();

        if (string.IsNullOrWhiteSpace(mac))
        {
            MessageBox.Show(
                "Please enter a device MAC address.",
                "Sensor Registration",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(deployment))
        {
            MessageBox.Show(
                "Please enter a deployment hierarchy.",
                "Sensor Registration",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            SensorCategory category;

            switch (CmbCategory.SelectedIndex)
            {
                case 0:
                    category = SensorCategory.Environmental;
                    break;

                case 1:
                    category = SensorCategory.PowerConsumption;
                    break;

                case 2:
                    category = SensorCategory.Actuator;
                    break;

                default:
                    MessageBox.Show(
                        "Please select a sensor category.",
                        "Sensor Registration",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
            }

            var sensor = new SensorProfile(
                mac,
                deployment,
                category);

            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    "/api/sensors",
                    sensor);

            if (response.IsSuccessStatusCode)
            {
                LogTelemetryFeed(
                    $"[REGISTERED] Sensor {mac} | {category} | {deployment}");

                LblSystemStatus.Text = "NORMAL";
                await LoadRegisteredSensorCount(); 

                MessageBox.Show(
                    "Sensor successfully registered.",
                    "Smart-X",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else if (response.StatusCode ==
                     System.Net.HttpStatusCode.Conflict)
            {
                LogTelemetryFeed(
                    $"[REGISTRATION REJECTED] Duplicate MAC address: {mac}");

                MessageBox.Show(
                    "A sensor with this MAC address is already registered.",
                    "Registration Conflict",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                LogTelemetryFeed(
                    $"[REGISTRATION ERROR] HTTP {response.StatusCode}");

                MessageBox.Show(
                    $"The API rejected the registration.\nHTTP Status: {response.StatusCode}",
                    "Registration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            LogTelemetryFeed(
                $"[CONNECTION ERROR] Sensor registration failed: {ex.Message}");

            MessageBox.Show(
                $"Could not connect to the Smart-X API.\n\n{ex.Message}",
                "Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void BtnSubmitTelemetry_Click(object sender, RoutedEventArgs e)
    {
        string mac = TxtMacAddress.Text.Trim();
        string deploymentRaw = TxtDeploymentPath.Text.Trim();
        string rawValue = TxtSensorValue.Text.Trim();

        try
        {
            HttpResponseMessage response = null;

            // Environmental telemetry (float)
            if (CmbCategory.SelectedIndex == 0)
            {
                if (!float.TryParse(rawValue, out float floatVal))
                {
                    MessageBox.Show(
                        "Please enter a valid decimal for Environmental data.",
                        "Format Error");
                    return;
                }

                var packet =
                    new TelemetryPacket<float>(
                        mac,
                        deploymentRaw,
                        floatVal);

                EvaluateVisualAnomaly(floatVal);

                response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/telemetry/environmental",
                        packet);

                if (response.IsSuccessStatusCode)
                {
                    UpdateTelemetryVisualization(
                        floatVal.ToString("F2"),
                        "Environmental");

                    AddTelemetryPoint(floatVal);
                }
            }

            // Power telemetry (int)
            else if (CmbCategory.SelectedIndex == 1)
            {
                if (!int.TryParse(rawValue, out int intVal))
                {
                    MessageBox.Show(
                        "Please enter a whole number for Power Consumption.",
                        "Format Error");
                    return;
                }

                var packet =
                    new TelemetryPacket<int>(
                        mac,
                        deploymentRaw,
                        intVal);

                response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/telemetry/power",
                        packet);

                if (response.IsSuccessStatusCode)
                {
                    UpdateTelemetryVisualization(
                        intVal.ToString(),
                        "Power");

                    AddTelemetryPoint(intVal);
                }
            }

            // Actuator telemetry (bool)
            else if (CmbCategory.SelectedIndex == 2)
            {
                if (!bool.TryParse(rawValue, out bool boolVal))
                {
                    MessageBox.Show(
                        "Please enter 'true' or 'false' for Actuator data.",
                        "Format Error");
                    return;
                }

                var packet =
                    new TelemetryPacket<bool>(
                        mac,
                        deploymentRaw,
                        boolVal);

                response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/telemetry/actuator",
                        packet);

                if (response.IsSuccessStatusCode)
                {
                    UpdateTelemetryVisualization(
                        boolVal ? "ON" : "OFF",
                        "Actuator");

                    AddTelemetryPoint(
                        boolVal ? 1 : 0);
                }
            }

            // API response handling
            if (response != null && response.IsSuccessStatusCode)
            {
                LogTelemetryFeed(
                    $"[INGESTED] MAC: {mac} | Value: {rawValue} | Type: {CmbCategory.Text.Split(' ')[0]}");

                LblPacketCount.Text =
                    (int.Parse(LblPacketCount.Text) + 1).ToString();
            }
            else
            {
                LogTelemetryFeed(
                    $"[API ERROR] HTTP {response?.StatusCode} received from gateway.");
            }
        }
        catch (Exception ex)
        {
            LogTelemetryFeed(
                $"[CONNECTION ERROR] Failed to push packet: {ex.Message}");
        }
    }
    private void UpdateTelemetryVisualization(string value, string category)
    {
        LblCurrentReading.Text = value;
        LblReadingCategory.Text = category + " telemetry";
        LblLastUpdate.Text = DateTime.Now.ToString("HH:mm:ss");
    }


    private void AddTelemetryPoint(double value)
    {
        // Remove old points when the chart gets crowded
        if (TelemetryChartCanvas.Children.Count > 40)
        {
            TelemetryChartCanvas.Children.RemoveAt(0);
        }

        double canvasWidth = TelemetryChartCanvas.ActualWidth;
        double canvasHeight = TelemetryChartCanvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0)
        {
            return;
        }

        // Convert the telemetry value into a vertical chart position.
        // Values are normalized between 0 and 100 for visualization.
        double normalizedValue = Math.Max(0, Math.Min(100, value));

        double x = TelemetryChartCanvas.Children.Count * 12;
        double y =
            canvasHeight -
            ((normalizedValue / 100.0) * canvasHeight);

        // Keep the point inside the canvas.
        if (x > canvasWidth)
        {
            x = canvasWidth - 5;
        }

        Ellipse point = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(32, 201, 151))
        };

        Canvas.SetLeft(point, x);
        Canvas.SetTop(point, y);

        TelemetryChartCanvas.Children.Add(point);
    }
    
    
    private void BtnSimulateAggregation_Click(object sender, RoutedEventArgs e)
    {
        TelemetryPacket<float> meterA = new TelemetryPacket<float>("00:1B:44:11:3A:01", "Sub-Zone B", 35.2f);
        TelemetryPacket<float> meterB = new TelemetryPacket<float>("00:1B:44:11:3A:02", "Sub-Zone B", 42.8f);

        TelemetryPacket<float> aggregatedMeter = meterA + meterB;

        LogTelemetryFeed($"[OPERATOR OVERLOAD +] Aggregated Load: {meterA.SensorValue} + {meterB.SensorValue} = {aggregatedMeter.SensorValue} (ID: {aggregatedMeter.DeviceMacAddress})");
    }

    private async void BtnFetchTelemetry_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/telemetry/environmental");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<float>>();
                int count = data != null ? data.Count : 0;
                LogTelemetryFeed($"[RETRIEVED] Fetched {count} optimized historical records from gateway API.");
            }
            else
            {
                LogTelemetryFeed($"[API ERROR] HTTP {response.StatusCode} during retrieval.");
            }
        }
        catch (Exception ex)
        {
            LogTelemetryFeed($"[FETCH ERROR] Failed to contact gateway: {ex.Message}");
        }
    }

    

    private void EvaluateVisualAnomaly(float value)
    {
        if (value > 85.0f || value < 10.0f)
        {
            LblStatusIndicator.Text = "STATUS: ANOMALOUS SPIKE DETECTED";
            LblStatusIndicator.Foreground = new SolidColorBrush(Colors.Red);
            LblAnomalyAlert.Text = $"Threshold exceeded: {value} units!";
            LblAnomalyAlert.Foreground = new SolidColorBrush(Colors.OrangeRed);
        }
        else
        {
            LblStatusIndicator.Text = "STATUS: NORMAL / OPTIMAL";
            LblStatusIndicator.Foreground = new SolidColorBrush(Colors.LightGreen);
            LblAnomalyAlert.Text = "Telemetry within expected baseline";
            LblAnomalyAlert.Foreground = new SolidColorBrush(Colors.LightGray);
        }
    }

    private void LogTelemetryFeed(string message)
    {
        TxtTelemetryFeed.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }
}