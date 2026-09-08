using Microsoft.Win32;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SmartXWPF;

public partial class SensorDashboardWindow : Window
{
    private readonly HttpClient _httpClient = new HttpClient();

    private string _attachedFilePath = string.Empty;

    // Custom dashboard feature:
    // Stores the latest readings displayed on the visual telemetry chart.
    private readonly List<double> _environmentalHistory = new();
    private readonly List<double> _powerHistory = new();
    private readonly List<double> _actuatorHistory = new();

    private int _packetCount = 0;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public SensorDashboardWindow()
    {
        InitializeComponent();

        string? apiBaseUrl =
            ConfigurationManager.AppSettings["ApiBaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new InvalidOperationException(
                "The Smart-X API base URL is not configured.");
        }

        _httpClient.BaseAddress =
            new Uri(apiBaseUrl);

        Loaded += async (sender, e) =>
        {
            await LoadRegisteredSensorCount();
        };
    }


    // ============================================================
    // SENSOR COUNT
    // ============================================================

    private async Task LoadRegisteredSensorCount()
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync("/api/sensors");

            if (response.IsSuccessStatusCode)
            {
                List<SensorProfile>? sensors =
                    await response.Content
                        .ReadFromJsonAsync<List<SensorProfile>>();

                int count =
                    sensors?.Count ?? 0;

                LblDeviceCount.Text =
                    count.ToString();

                SetGatewayStatus(true);

                LogTelemetryFeed(
                    $"[SYSTEM] Gateway connection established. Loaded {count} registered sensor profile(s).");
            }
            else
            {
                SetGatewayStatus(false);

                LogTelemetryFeed(
                    $"[SYSTEM WARNING] Gateway returned HTTP {response.StatusCode} while loading registered sensors.");
            }
        }
        catch
        {
            SetGatewayStatus(false);

            LogTelemetryFeed(
                "[SYSTEM WARNING] Could not connect to the Smart-X API while loading registered sensors.");
        }
    }
    private void SetGatewayStatus(bool isOnline)
    {
        if (isOnline)
        {
            LblGatewayStatus.Text =
                "GATEWAY ONLINE";

            LblGatewayStatus.Foreground =
                new SolidColorBrush(
                    Colors.LightGreen);

            LblSystemStatus.Text =
                "NORMAL";

            LblSystemStatus.Foreground =
                new SolidColorBrush(
                    Colors.LightGreen);
        }
        else
        {
            LblGatewayStatus.Text =
                "GATEWAY OFFLINE";

            LblGatewayStatus.Foreground =
                new SolidColorBrush(
                    Colors.Red);

            LblSystemStatus.Text =
                "GATEWAY OFFLINE";

            LblSystemStatus.Foreground =
                new SolidColorBrush(
                    Colors.Red);
        }
    }

    // ============================================================
    // FILE ATTACHMENT
    // ============================================================

    private async void BtnAttachFile_Click(
        object sender,
        RoutedEventArgs e)
    {
        string mac =
            TxtMacAddress.Text.Trim();

        if (string.IsNullOrWhiteSpace(mac))
        {
            MessageBox.Show(
                "Please enter and register a sensor before attaching a file.",
                "Sensor Attachment",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        OpenFileDialog openFileDialog =
            new OpenFileDialog
            {
                Filter =
                    "Config & Log Files (*.json;*.txt;*.log;*.png;*.jpg)|*.json;*.txt;*.log;*.png;*.jpg|All Files (*.*)|*.*",

                Title =
                    "Select Sensor Profile Attachment"
            };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _attachedFilePath =
                openFileDialog.FileName;

            using MultipartFormDataContent form =
                new MultipartFormDataContent();

            using FileStream fileStream =
                File.OpenRead(_attachedFilePath);

            using StreamContent fileContent =
                new StreamContent(fileStream);

            string fileName =
     System.IO.Path.GetFileName(_attachedFilePath);

            form.Add(
                fileContent,
                "file",
                fileName);

            HttpResponseMessage response =
                await _httpClient.PostAsync(
                    $"/api/sensors/{Uri.EscapeDataString(mac)}/attachment",
                    form);

            if (response.IsSuccessStatusCode)
            {
                long fileSizeKb =
                    new FileInfo(
                        _attachedFilePath).Length / 1024;

                LblAttachedFile.Text =
                    $"Attached: {fileName} ({fileSizeKb} KB)";

                LblAttachedFile.Foreground =
                    new SolidColorBrush(
                        Colors.LightGreen);

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
                LogTelemetryFeed(
                    $"[ATTACHMENT ERROR] HTTP {response.StatusCode}");

                MessageBox.Show(
                    $"The API rejected the attachment.\n\nHTTP Status: {response.StatusCode}",
                    "Attachment Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch
        {
            LogTelemetryFeed(
                "[ATTACHMENT ERROR] Could not upload attachment.");

            MessageBox.Show(
                "Could not upload the attachment. Please verify that the API is running and try again.",
                "Attachment Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CmbCategory_SelectionChanged(
    object sender,
    System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (CmbCategory == null ||
            TxtSensorValue == null ||
            CmbActuatorState == null)
        {
            return;
        }

        bool isActuator =
            CmbCategory.SelectedIndex == 2;

        if (isActuator)
        {
            TxtSensorValue.Visibility =
                Visibility.Collapsed;

            CmbActuatorState.Visibility =
                Visibility.Visible;
        }
        else
        {
            TxtSensorValue.Visibility =
                Visibility.Visible;

            CmbActuatorState.Visibility =
                Visibility.Collapsed;

            if (CmbCategory.SelectedIndex == 0)
            {
                TxtSensorValue.Text = "45.5";
            }
            else if (CmbCategory.SelectedIndex == 1)
            {
                TxtSensorValue.Text = "742";
            }
        }
    }
    
    // SENSOR REGISTRATION
   

    private async void BtnRegisterSensor_Click(
        object sender,
        RoutedEventArgs e)
    {
        string mac =
            TxtMacAddress.Text.Trim();

        string deployment =
            TxtDeploymentPath.Text.Trim();

        if (string.IsNullOrWhiteSpace(mac))
        {
            MessageBox.Show(
                "Please enter a device MAC address.",
                "Sensor Registration",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (mac.Length > 17)
        {
            MessageBox.Show(
                "The device MAC address is too long.",
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

        if (deployment.Length > 200)
        {
            MessageBox.Show(
                "The deployment hierarchy is too long.",
                "Sensor Registration",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        SensorCategory category;

        switch (CmbCategory.SelectedIndex)
        {
            case 0:
                category =
                    SensorCategory.Environmental;
                break;

            case 1:
                category =
                    SensorCategory.PowerConsumption;
                break;

            case 2:
                category =
                    SensorCategory.Actuator;
                break;

            default:
                MessageBox.Show(
                    "Please select a sensor category.",
                    "Sensor Registration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
        }

        try
        {
            SensorProfile sensor =
                new SensorProfile(
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

                LblSystemStatus.Text =
                    "NORMAL";

                await LoadRegisteredSensorCount();

                MessageBox.Show(
                    "Sensor successfully registered.",
                    "Smart-X",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else if (
                response.StatusCode ==
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
        catch
        {
            LogTelemetryFeed(
                "[CONNECTION ERROR] Sensor registration failed.");

            MessageBox.Show(
                "Could not connect to the Smart-X API.",
                "Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    // ============================================================
    // TELEMETRY SUBMISSION
    // ============================================================

    private async void BtnSubmitTelemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        string mac =
            TxtMacAddress.Text.Trim();

        string deployment =
            TxtDeploymentPath.Text.Trim();

        string rawValue =
            TxtSensorValue.Text.Trim();


        if (string.IsNullOrWhiteSpace(mac))
        {
            MessageBox.Show(
                "Please enter a device MAC address.",
                "Telemetry",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(deployment))
        {
            MessageBox.Show(
                "Please enter a deployment hierarchy.",
                "Telemetry",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

       

        try
        {
            HttpResponseMessage? response = null;

            SensorCategory category;

            switch (CmbCategory.SelectedIndex)
            {
                // ------------------------------------------------
                // ENVIRONMENTAL
                // ------------------------------------------------

                case 0:

                    category =
                        SensorCategory.Environmental;

                    if (!float.TryParse(
                            rawValue,
                            out float floatValue))
                    {
                        MessageBox.Show(
                            "Please enter a valid decimal for Environmental data.",
                            "Format Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    if (float.IsNaN(floatValue) ||
                        float.IsInfinity(floatValue))
                    {
                        MessageBox.Show(
                            "Environmental sensor value must be a valid number.",
                            "Format Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    TelemetryPacket<float> environmentalPacket =
                        new TelemetryPacket<float>(
                            mac,
                            deployment,
                            floatValue);

                    response =
                        await _httpClient.PostAsJsonAsync(
                            "/api/telemetry/environmental",
                            environmentalPacket);

                    // IMPORTANT:
                    // The visual anomaly state is updated only
                    // after the API accepts the packet.
                    if (response.IsSuccessStatusCode)
                    {
                        UpdateTelemetryVisualization(
                            floatValue.ToString("F2"),
                            SensorCategory.Environmental);

                        AddTelemetryPoint(
                            floatValue,
                            SensorCategory.Environmental);

                        EvaluateVisualAnomaly(
                            floatValue);
                    }

                    break;


                // ------------------------------------------------
                // POWER
                // ------------------------------------------------

                case 1:

                    category =
                        SensorCategory.PowerConsumption;

                    if (!int.TryParse(
                            rawValue,
                            out int intValue))
                    {
                        MessageBox.Show(
                            "Please enter a whole number for Power Consumption.",
                            "Format Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    TelemetryPacket<int> powerPacket =
                        new TelemetryPacket<int>(
                            mac,
                            deployment,
                            intValue);

                    response =
                        await _httpClient.PostAsJsonAsync(
                            "/api/telemetry/power",
                            powerPacket);

                    if (response.IsSuccessStatusCode)
                    {
                        UpdateTelemetryVisualization(
                            intValue.ToString(),
                            SensorCategory.PowerConsumption);

                        AddTelemetryPoint(
                            intValue,
                            SensorCategory.PowerConsumption);

                        EvaluatePowerAnomaly(
                            intValue);
                    }

                    break;


                // ------------------------------------------------
                // ACTUATOR
                // ------------------------------------------------

                case 2:

                    category =
                        SensorCategory.Actuator;

                    if (CmbActuatorState.SelectedItem is not ComboBoxItem actuatorItem)
                    {
                        MessageBox.Show(
                            "Please select an actuator state.",
                            "Format Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    string actuatorState =
                        actuatorItem.Content?.ToString() ?? string.Empty;

                    bool boolValue =
                        actuatorState.Equals(
                            "ON",
                            StringComparison.OrdinalIgnoreCase);

                    TelemetryPacket<bool> actuatorPacket =
                        new TelemetryPacket<bool>(
                            mac,
                            deployment,
                            boolValue);

                    response =
                        await _httpClient.PostAsJsonAsync(
                            "/api/telemetry/actuator",
                            actuatorPacket);

                    if (response.IsSuccessStatusCode)
                    {
                        UpdateTelemetryVisualization(
                            boolValue ? "ON" : "OFF",
                            SensorCategory.Actuator);

                        AddTelemetryPoint(
                            boolValue ? 1 : 0,
                            SensorCategory.Actuator);
                    }

                    break;


                default:

                    MessageBox.Show(
                        "Please select a sensor category.",
                        "Telemetry",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
            }


            // ----------------------------------------------------
            // API RESPONSE
            // ----------------------------------------------------

            if (response != null &&
                response.IsSuccessStatusCode)
            {
                _packetCount++;

                LblPacketCount.Text =
                    _packetCount.ToString();

                LogTelemetryFeed(
                    $"[INGESTED] MAC: {mac} | Value: {rawValue} | Type: {category}");
            }
            else
            {
                SetGatewayStatus(
                    response != null);

                LogTelemetryFeed(
                    $"[API ERROR] HTTP {response?.StatusCode} received from gateway.");

                MessageBox.Show(
                    $"The Smart-X API rejected the telemetry packet.\nHTTP Status: {response?.StatusCode}",
                    "Telemetry Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch
        {
            SetGatewayStatus(false);

            LogTelemetryFeed(
                "[CONNECTION ERROR] Failed to push telemetry packet.");

            MessageBox.Show(
                "Could not connect to the Smart-X API.",
                "Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    // ============================================================
    // MOCK TELEMETRY SEEDING
    // ============================================================

    private async void BtnSeedTelemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        string mac =
            TxtMacAddress.Text.Trim();

        string deployment =
            TxtDeploymentPath.Text.Trim();

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
            Random random =
                new Random();

            const int environmentalCount = 100;
            const int powerCount = 100;
            const int actuatorCount = 100;

            int successfulPackets = 0;

            LogTelemetryFeed(
                $"[SEED START] Generating {environmentalCount + powerCount + actuatorCount} mock telemetry packets...");


            // ----------------------------------------------------
            // ENVIRONMENTAL
            // ----------------------------------------------------

            for (
                int i = 0;
                i < environmentalCount;
                i++)
            {
                float value =
                    40.0f +
                    (float)(random.NextDouble() * 30.0);

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
                        SensorCategory.Environmental);

                    AddTelemetryPoint(
                        value,
                        SensorCategory.Environmental);

                    IncrementPacketCount();
                }
            }


            // ----------------------------------------------------
            // POWER
            // ----------------------------------------------------

            for (
                int i = 0;
                i < powerCount;
                i++)
            {
                int value =
                    random.Next(200, 1001);

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
                        SensorCategory.PowerConsumption);

                    // FIX:
                    // Uses the enum rather than "power"/"Power"
                    // strings, preventing category mix-ups.
                    AddTelemetryPoint(
                        value,
                        SensorCategory.PowerConsumption);

                    IncrementPacketCount();
                }
            }


            // ----------------------------------------------------
            // ACTUATOR
            // ----------------------------------------------------

            for (
                int i = 0;
                i < actuatorCount;
                i++)
            {
                bool value =
                    random.Next(0, 2) == 1;

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
                        SensorCategory.Actuator);

                    AddTelemetryPoint(
                        value ? 1 : 0,
                        SensorCategory.Actuator);

                    IncrementPacketCount();
                }
            }


            LogTelemetryFeed(
                $"[SEED COMPLETE] Successfully processed {successfulPackets} mock telemetry packets.");

            MessageBox.Show(
                $"Mock telemetry seeding completed.\n\nSuccessful packets: {successfulPackets}",
                "Smart-X",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch
        {
            LogTelemetryFeed(
                "[SEED ERROR] Mock telemetry generation failed.");

            MessageBox.Show(
                "Mock telemetry seeding failed. Please verify that the Smart-X API is running.",
                "Mock Telemetry Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    // ============================================================
    // DASHBOARD VISUALISATION
    // ============================================================

    private void UpdateTelemetryVisualization(
        string value,
        SensorCategory category)
    {
        LblCurrentReading.Text =
            value;

        LblReadingCategory.Text =
            GetCategoryDisplayName(category) +
            " telemetry";

        LblLastUpdate.Text =
            DateTime.Now.ToString("HH:mm:ss");
    }


    private void AddTelemetryPoint(
        double value,
        SensorCategory category)
    {
        List<double> history =
            GetHistoryCollection(category);

        history.Add(value);

        // Keep only the latest 30 readings
        // for the visual dashboard chart.
        const int maxChartReadings = 30;

        if (history.Count > maxChartReadings)
        {
            history.RemoveAt(0);
        }

        DrawTelemetryChart(
            history,
            category);
    }


    private List<double> GetHistoryCollection(
        SensorCategory category)
    {
        return category switch
        {
            SensorCategory.Environmental =>
                _environmentalHistory,

            SensorCategory.PowerConsumption =>
                _powerHistory,

            SensorCategory.Actuator =>
                _actuatorHistory,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(category))
        };
    }


    private string GetCategoryDisplayName(
        SensorCategory category)
    {
        return category switch
        {
            SensorCategory.Environmental =>
                "Environmental",

            SensorCategory.PowerConsumption =>
                "Power",

            SensorCategory.Actuator =>
                "Actuator",

            _ =>
                "Unknown"
        };
    }


    // ============================================================
    // CUSTOM VISUAL TELEMETRY CHART
    // ============================================================

    private void DrawTelemetryChart(
        List<double> history,
        SensorCategory category)
    {
        TelemetryChartCanvas.Children.Clear();

        double width =
            TelemetryChartCanvas.ActualWidth;

        double height =
            TelemetryChartCanvas.ActualHeight;

        if (width <= 0 ||
            height <= 0 ||
            history.Count == 0)
        {
            return;
        }

        double minimum;
        double maximum;


        // Actuator telemetry is boolean,
        // so the chart uses a fixed 0-1 scale.
        if (category ==
            SensorCategory.Actuator)
        {
            minimum = 0;
            maximum = 1;
        }
        else
        {
            minimum =
                history.Min();

            maximum =
                history.Max();

            // Prevent a completely flat graph.
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


        for (
            int i = 0;
            i < history.Count;
            i++)
        {
            double value =
                history[i];

            double normalized =
                (value - minimum) /
                (maximum - minimum);

            normalized =
                Math.Max(
                    0,
                    Math.Min(
                        1,
                        normalized));

            double x =
                i * horizontalStep;

            double y =
                height -
                (normalized * height);


            // Draw reading point.
            Ellipse point =
                new Ellipse
                {
                    Width = 6,
                    Height = 6,

                    Fill =
                        new SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(
                                32,
                                201,
                                151))
                };

            Canvas.SetLeft(
                point,
                x - 3);

            Canvas.SetTop(
                point,
                y - 3);

            TelemetryChartCanvas.Children.Add(
                point);


            // Connect reading to previous reading.
            if (i > 0)
            {
                double previousValue =
                    history[i - 1];

                double previousNormalized =
                    (previousValue - minimum) /
                    (maximum - minimum);

                previousNormalized =
                    Math.Max(
                        0,
                        Math.Min(
                            1,
                            previousNormalized));

                double previousX =
                    (i - 1) *
                    horizontalStep;

                double previousY =
                    height -
                    (previousNormalized * height);


                Line line =
                    new Line
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


                // Place line behind the point.
                TelemetryChartCanvas.Children.Insert(
                    TelemetryChartCanvas.Children.Count - 1,
                    line);
            }
        }
    }


    // ============================================================
    // OPERATOR OVERLOADING DEMONSTRATION
    // ============================================================

    private void BtnSimulateAggregation_Click(
        object sender,
        RoutedEventArgs e)
    {
        TelemetryPacket<float> meterA =
            new TelemetryPacket<float>(
                "00:1B:44:11:3A:01",
                "Sub-Zone B",
                35.2f);

        TelemetryPacket<float> meterB =
            new TelemetryPacket<float>(
                "00:1B:44:11:3A:02",
                "Sub-Zone B",
                42.8f);

        TelemetryPacket<float> aggregatedMeter =
            meterA + meterB;

        LogTelemetryFeed(
            $"[OPERATOR OVERLOAD +] Aggregated Load: " +
            $"{meterA.SensorValue} + " +
            $"{meterB.SensorValue} = " +
            $"{aggregatedMeter.SensorValue} " +
            $"(ID: {aggregatedMeter.DeviceMacAddress})");
    }


    // ============================================================
    // TELEMETRY RETRIEVAL
    // ============================================================

    private async void BtnFetchTelemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(
                    "/api/telemetry/environmental");

            if (response.IsSuccessStatusCode)
            {
                List<float>? data =
                    await response.Content
                        .ReadFromJsonAsync<List<float>>();

                int count =
                    data?.Count ?? 0;

                LogTelemetryFeed(
                    $"[RETRIEVED] Fetched {count} optimized historical records from gateway API.");
            }
            else
            {
                LogTelemetryFeed(
                    $"[API ERROR] HTTP {response.StatusCode} during retrieval.");
            }
        }
        catch
        {
            LogTelemetryFeed(
                "[FETCH ERROR] Failed to contact gateway.");
        }
    }


    // ============================================================
    // DYNAMIC ENGAGEMENT / ANOMALY DETECTION
    // ============================================================

    private void EvaluateVisualAnomaly(
        float value)
    {
        if (value > 85.0f ||
            value < 10.0f)
        {
            LblStatusIndicator.Text =
                "STATUS: ANOMALOUS SPIKE DETECTED";

            LblStatusIndicator.Foreground =
                new SolidColorBrush(
                    Colors.Red);

            LblAnomalyAlert.Text =
                $"Threshold exceeded: {value} units!";

            LblAnomalyAlert.Foreground =
                new SolidColorBrush(
                    Colors.OrangeRed);
        }
        else
        {
            LblStatusIndicator.Text =
                "STATUS: NORMAL / OPTIMAL";

            LblStatusIndicator.Foreground =
                new SolidColorBrush(
                    Colors.LightGreen);

            LblAnomalyAlert.Text =
                "Telemetry within expected baseline";

            LblAnomalyAlert.Foreground =
                new SolidColorBrush(
                    Colors.LightGray);
        }
    }
    private void EvaluatePowerAnomaly(int value)
    {
        if (value > 8000)
        {
            LblStatusIndicator.Text =
                "STATUS: HIGH POWER CONSUMPTION";

            LblStatusIndicator.Foreground =
                new SolidColorBrush(
                    Colors.Red);

            LblAnomalyAlert.Text =
                $"Power consumption above threshold: {value} units";

            LblAnomalyAlert.Foreground =
                new SolidColorBrush(
                    Colors.OrangeRed);
        }
        else
        {
            LblStatusIndicator.Text =
                "STATUS: NORMAL / OPTIMAL";

            LblStatusIndicator.Foreground =
                new SolidColorBrush(
                    Colors.LightGreen);

            LblAnomalyAlert.Text =
                "Power consumption within expected baseline";

            LblAnomalyAlert.Foreground =
                new SolidColorBrush(
                    Colors.LightGray);
        }
    }

    // ============================================================
    // PACKET COUNT
    // ============================================================

    private void IncrementPacketCount()
    {
        _packetCount++;

        LblPacketCount.Text =
            _packetCount.ToString();
    }


    // ============================================================
    // TELEMETRY LOG
    // ============================================================

    private void LogTelemetryFeed(
        string message)
    {
        TxtTelemetryFeed.Text +=
            $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }
}