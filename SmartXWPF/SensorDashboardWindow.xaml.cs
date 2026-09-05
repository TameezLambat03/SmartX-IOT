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
    private readonly List<double> _environmentalHistory = new();
    private readonly List<double> _powerHistory = new();
    private readonly List<double> _actuatorHistory = new();




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

    private async void BtnAttachFile_Click(object sender, RoutedEventArgs e)
    {
        string mac = TxtMacAddress.Text.Trim();

        if (string.IsNullOrWhiteSpace(mac))
        {
            MessageBox.Show(
                "Please enter and register a sensor before attaching a file.",
                "Sensor Attachment",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Config & Log Files (*.json;*.txt;*.log;*.png;*.jpg)|*.json;*.txt;*.log;*.png;*.jpg|All Files (*.*)|*.*",
            Title = "Select Sensor Profile Attachment"
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _attachedFilePath = openFileDialog.FileName;

            using MultipartFormDataContent form =
                new MultipartFormDataContent();

            using FileStream fileStream =
                File.OpenRead(_attachedFilePath);

            using StreamContent fileContent =
                new StreamContent(fileStream);

            form.Add(
                fileContent,
                "file",
                System.IO.Path.GetFileName(_attachedFilePath));

            HttpResponseMessage response =
                await _httpClient.PostAsync(
                    $"/api/sensors/{Uri.EscapeDataString(mac)}/attachment",
                    form);

            if (response.IsSuccessStatusCode)
            {
                string fileName =
                    System.IO.Path.GetFileName(_attachedFilePath);

                long fileSizeKb =
                    new FileInfo(_attachedFilePath).Length / 1024;

                LblAttachedFile.Text =
                    $"Attached: {fileName} ({fileSizeKb} KB)";

                LblAttachedFile.Foreground =
                    new SolidColorBrush(Colors.LightGreen);

                LogTelemetryFeed(
                    $"[ATTACHMENT UPLOADED] {fileName} → Sensor {mac}");

                MessageBox.Show(
                    "File successfully attached to the sensor profile.",
                    "Smart-X",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                string errorMessage =
                    await response.Content.ReadAsStringAsync();

                LogTelemetryFeed(
                    $"[ATTACHMENT ERROR] HTTP {response.StatusCode}");

                MessageBox.Show(
                    $"The API rejected the attachment.\n\nHTTP Status: {response.StatusCode}\n{errorMessage}",
                    "Attachment Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            LogTelemetryFeed(
                $"[ATTACHMENT ERROR] {ex.Message}");

            MessageBox.Show(
                $"Could not upload the attachment.\n\n{ex.Message}",
                "Attachment Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

                    AddTelemetryPoint(floatVal, "Environmental");

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

                    AddTelemetryPoint(intVal,"Power");
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
                        boolVal ? 1 : 0,"Actuator");
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
    private async void BtnSeedTelemetry_Click(object sender, RoutedEventArgs e)
    {
        string mac = TxtMacAddress.Text.Trim();
        string deployment = TxtDeploymentPath.Text.Trim();

        if (string.IsNullOrWhiteSpace(mac))
        {
            MessageBox.Show(
                "Please enter a sensor MAC address before seeding telemetry.",
                "Mock Telemetry",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(deployment))
        {
            MessageBox.Show(
                "Please enter a deployment location before seeding telemetry.",
                "Mock Telemetry",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            Random random = new Random();

            int environmentalCount = 100;
            int powerCount = 100;
            int actuatorCount = 100;

            int successfulPackets = 0;

            LogTelemetryFeed(
                $"[SEED START] Generating {environmentalCount + powerCount + actuatorCount} mock telemetry packets...");

            // Environmental telemetry
            for (int i = 0; i < environmentalCount; i++)
            {
                float value = 40.0f + (float)(random.NextDouble() * 30.0);

                TelemetryPacket<float> packet =
                    new TelemetryPacket<float>(
                        mac,
                        deployment,
                        value);

                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/telemetry/environmental",
                        packet);

                if (response.IsSuccessStatusCode)
                {
                    successfulPackets++;

                    UpdateTelemetryVisualization(
                        value.ToString("F2"),
                        "Environmental");

                    AddTelemetryPoint(value,"Environmental");

                    LblPacketCount.Text =
                        (int.Parse(LblPacketCount.Text) + 1).ToString();
                }
            }

            // Power telemetry
            for (int i = 0; i < powerCount; i++)
            {
                int value = random.Next(200, 1001);

                TelemetryPacket<int> packet =
                    new TelemetryPacket<int>(
                        mac,
                        deployment,
                        value);

                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/telemetry/power",
                        packet);

                if (response.IsSuccessStatusCode)
                {
                    successfulPackets++;

                    UpdateTelemetryVisualization(
                        value.ToString(),
                        "Power");

                    AddTelemetryPoint(value,"power");

                    LblPacketCount.Text =
                        (int.Parse(LblPacketCount.Text) + 1).ToString();
                }
            }

            // Actuator telemetry
            for (int i = 0; i < actuatorCount; i++)
            {
                bool value = random.Next(0, 2) == 1;

                TelemetryPacket<bool> packet =
                    new TelemetryPacket<bool>(
                        mac,
                        deployment,
                        value);

                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/telemetry/actuator",
                        packet);

                if (response.IsSuccessStatusCode)
                {
                    successfulPackets++;

                    UpdateTelemetryVisualization(
                        value ? "ON" : "OFF",
                        "Actuator");

                    AddTelemetryPoint(
                        value ? 1 : 0,"Actuator");

                    LblPacketCount.Text =
                        (int.Parse(LblPacketCount.Text) + 1).ToString();
                }
            }
            DrawTelemetryChart(
    _environmentalHistory,
    "Environmental");
            LogTelemetryFeed(
                $"[SEED COMPLETE] Successfully processed {successfulPackets} mock telemetry packets.");

            MessageBox.Show(
                $"Mock telemetry seeding completed.\n\nSuccessful packets: {successfulPackets}",
                "Smart-X",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogTelemetryFeed(
                $"[SEED ERROR] {ex.Message}");

            MessageBox.Show(
                $"Mock telemetry seeding failed.\n\n{ex.Message}",
                "Mock Telemetry Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void UpdateTelemetryVisualization(string value, string category)
    {
        LblCurrentReading.Text = value;
        LblReadingCategory.Text = category + " telemetry";
        LblLastUpdate.Text = DateTime.Now.ToString("HH:mm:ss");
    }


    private void AddTelemetryPoint(double value, string category)
    {
        List<double> history;

        if (category == "Environmental")
        {
            history = _environmentalHistory;
        }
        else if (category == "Power")
        {
            history = _powerHistory;
        }
        else
        {
            history = _actuatorHistory;
        }

        history.Add(value);

        // Keep only the latest 30 readings for the visual chart.
        if (history.Count > 30)
        {
            history.RemoveAt(0);
        }

        DrawTelemetryChart(history, category);
    }
    private void DrawTelemetryChart(
    List<double> history,
    string category)
    {
        TelemetryChartCanvas.Children.Clear();

        double width = TelemetryChartCanvas.ActualWidth;
        double height = TelemetryChartCanvas.ActualHeight;

        if (width <= 0 || height <= 0 || history.Count == 0)
        {
            return;
        }

        double minimum;
        double maximum;

        // Give each telemetry category an appropriate scale.
        if (category == "Actuator")
        {
            minimum = 0;
            maximum = 1;
        }
        else
        {
            minimum = history.Min();
            maximum = history.Max();

            // Prevent a completely flat graph when all values are similar.
            if (Math.Abs(maximum - minimum) < 0.01)
            {
                minimum -= 1;
                maximum += 1;
            }
        }

        double horizontalStep =
            history.Count > 1
                ? width / (history.Count - 1)
                : width;

        for (int i = 0; i < history.Count; i++)
        {
            double value = history[i];

            double normalized =
                (value - minimum) / (maximum - minimum);

            normalized =
                Math.Max(0, Math.Min(1, normalized));

            double x =
                i * horizontalStep;

            double y =
                height - (normalized * height);

            Ellipse point = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(
                        32,
                        201,
                        151))
            };

            Canvas.SetLeft(point, x - 3);
            Canvas.SetTop(point, y - 3);

            TelemetryChartCanvas.Children.Add(point);

            // Connect this point to the previous point.
            if (i > 0)
            {
                double previousValue = history[i - 1];

                double previousNormalized =
                    (previousValue - minimum) /
                    (maximum - minimum);

                previousNormalized =
                    Math.Max(
                        0,
                        Math.Min(1, previousNormalized));

                double previousX =
                    (i - 1) * horizontalStep;

                double previousY =
                    height -
                    (previousNormalized * height);

                Line line = new Line
                {
                    X1 = previousX,
                    Y1 = previousY,
                    X2 = x,
                    Y2 = y,
                    Stroke =
                        new SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(
                                32,
                                201,
                                151)),
                    StrokeThickness = 2
                };

                // Put the line behind the points.
                TelemetryChartCanvas.Children.Insert(
                    TelemetryChartCanvas.Children.Count - 1,
                    line);
            }
        }
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