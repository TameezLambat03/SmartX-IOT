using Microsoft.Win32;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using SmartXWPF.Services;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SmartXWPF.Dashboard;

public class DashboardController
{
    private readonly DashboardView _view;
    private readonly DashboardState _state;

    private readonly SensorApiService _sensorApi;
    private readonly TelemetryApiService _telemetryApi;
    private readonly AttachmentService _attachmentService;
    private readonly MockTelemetryService _mockService;
    private readonly AnomalyDetector _anomalyDetector;
    private readonly TelemetryChartService _chart;

    public DashboardController(
        DashboardView view,
        ApiService apiService,
        TelemetryChartService chart)
    {
        _view = view;
        _state = new DashboardState();

        _sensorApi =
            new SensorApiService(apiService);

        _telemetryApi =
            new TelemetryApiService(apiService);

        _attachmentService =
            new AttachmentService(apiService);

        _mockService =
            new MockTelemetryService();

        _anomalyDetector =
            new AnomalyDetector();

        _chart = chart;
    }

    public async Task InitializeAsync()
    {
        await RefreshSensorsAsync();
        await LoadHistoricalTelemetryAsync();
    }

    public async Task RefreshSensorsAsync()
    {
        try
        {
            var result =
                await _sensorApi.GetSensorsAsync();

            if (!result.Success)
            {
                _view.SetGatewayStatus(false);

                _view.Log(
                    "[SYSTEM WARNING] Gateway returned an error while loading registered sensors.");

                return;
            }

            _view.SetSensors(result.Sensors);
            _view.SetGatewayStatus(true);

            _view.Log(
                $"[SYSTEM] Gateway connection established. " +
                $"Loaded {result.Sensors.Count} registered sensor profile(s).");
        }
        catch
        {
            _view.SetGatewayStatus(false);

            _view.Log(
                "[SYSTEM WARNING] Could not connect to the Smart-X API.");
        }
    }

    public void SelectSensor(
        SensorProfile? sensor)
    {
        if (sensor == null)
        {
            return;
        }

        _state.SelectedSensor = sensor;

        _view.DisplaySelectedSensor(sensor);

        _view.Log(
            $"[SENSOR SELECTED] " +
            $"{sensor.DeviceMacAddress} | " +
            $"{sensor.Category} | " +
            $"{sensor.DeploymentLocation}");
    }

    public void UpdateCategoryControls(
        int selectedIndex)
    {
        _view.UpdateCategoryControls(selectedIndex);
    }
    public void ShowGraph(
    SensorCategory category)
    {
        _chart.ShowHistory(category);
    }

    public async Task RegisterSensorAsync()
    {
        string mac =
            _view.GetMac();

        string deployment =
            _view.GetDeployment();

        SensorCategory? category =
            _view.GetSelectedCategory();

        if (string.IsNullOrWhiteSpace(mac))
        {
            Warn("Please enter a sensor MAC address.");
            return;
        }

        if (mac.Length > 17)
        {
            Warn("The MAC address cannot exceed 17 characters.");
            return;
        }

        if (string.IsNullOrWhiteSpace(deployment))
        {
            Warn("Please enter a deployment location.");
            return;
        }

        if (deployment.Length > 200)
        {
            Warn("The deployment location cannot exceed 200 characters.");
            return;
        }

        if (category == null)
        {
            Warn("Please select a sensor category.");
            return;
        }

        try
        {
            SensorProfile sensor =
                new SensorProfile(
                    mac,
                    deployment,
                    category.Value);

            var response =
                await _sensorApi.RegisterSensorAsync(sensor);

            if (response.IsSuccessStatusCode)
            {
                _view.Log(
                    $"[REGISTERED] Sensor {mac} | " +
                    $"{category} | {deployment}");

                await RefreshSensorsAsync();

                System.Windows.MessageBox.Show(
                    "Sensor successfully registered.",
                    "Smart-X",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else if (response.StatusCode ==
                     HttpStatusCode.Conflict)
            {
                _view.Log(
                    $"[REGISTRATION REJECTED] Duplicate MAC address: {mac}");

                System.Windows.MessageBox.Show(
                    "A sensor with this MAC address is already registered.",
                    "Registration Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
            else
            {
                _view.Log(
                    $"[REGISTRATION ERROR] HTTP {response.StatusCode}");

                System.Windows.MessageBox.Show(
                    $"The API rejected the registration.\nHTTP Status: {response.StatusCode}",
                    "Registration Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        catch
        {
            _view.Log(
                "[CONNECTION ERROR] Sensor registration failed.");

            System.Windows.MessageBox.Show(
                "Could not connect to the Smart-X API.",
                "Connection Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public async Task SubmitTelemetryAsync()
    {
        SensorProfile? sensor =
            _state.SelectedSensor;

        if (sensor == null)
        {
            Warn("Please select a registered sensor first.");
            return;
        }

        try
        {
            bool success;

            switch (sensor.Category)
            {
                case SensorCategory.Environmental:
                    success =
                        await SubmitEnvironmentalAsync(sensor);
                    break;

                case SensorCategory.PowerConsumption:
                    success =
                        await SubmitPowerAsync(sensor);
                    break;

                case SensorCategory.Actuator:
                    success =
                        await SubmitActuatorAsync(sensor);
                    break;

                default:
                    return;
            }

            if (success)
            {
                _state.IncrementPacketCount();

                _view.IncrementPacketCount(
                    _state.PacketCount);

                _view.Log(
                    $"[INGESTED] Sensor: {sensor.DeviceMacAddress} | " +
                    $"Type: {sensor.Category}");
            }
        }
        catch
        {
            _view.Log(
                "[CONNECTION ERROR] Failed to push telemetry packet.");

            _view.SetGatewayStatus(false);

            System.Windows.MessageBox.Show(
                "Could not connect to the Smart-X API.",
                "Telemetry Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task<bool> SubmitEnvironmentalAsync(
        SensorProfile sensor)
    {
        string raw =
            _view.GetTelemetryValue();

        if (!float.TryParse(raw, out float value) ||
            float.IsNaN(value) ||
            float.IsInfinity(value))
        {
            Warn("Please enter a valid environmental value.");
            return false;
        }

        TelemetryPacket<float> packet =
            new TelemetryPacket<float>(
                sensor.DeviceMacAddress,
                sensor.DeploymentLocation,
                value);

        var response =
            await _telemetryApi.SendEnvironmentalAsync(packet);

        if (!response.IsSuccessStatusCode)
        {
            HandleTelemetryError(response);
            return false;
        }

        _view.UpdateCurrentReading(
            value.ToString("F2"),
            SensorCategory.Environmental);

        _chart.AddPoint(
            value,
            SensorCategory.Environmental);

        _view.ApplyAnomaly(
            _anomalyDetector.EvaluateEnvironmental(value));

        return true;
    }

    private async Task<bool> SubmitPowerAsync(
        SensorProfile sensor)
    {
        string raw =
            _view.GetTelemetryValue();

        if (!int.TryParse(raw, out int value))
        {
            Warn("Please enter a valid power value.");
            return false;
        }

        TelemetryPacket<int> packet =
            new TelemetryPacket<int>(
                sensor.DeviceMacAddress,
                sensor.DeploymentLocation,
                value);

        var response =
            await _telemetryApi.SendPowerAsync(packet);

        if (!response.IsSuccessStatusCode)
        {
            HandleTelemetryError(response);
            return false;
        }

        _view.UpdateCurrentReading(
            value.ToString(),
            SensorCategory.PowerConsumption);

        _chart.AddPoint(
            value,
            SensorCategory.PowerConsumption);

        _view.ApplyAnomaly(
            _anomalyDetector.EvaluatePower(value));

        return true;
    }

    private async Task<bool> SubmitActuatorAsync(
        SensorProfile sensor)
    {
        bool? value =
            _view.GetActuatorValue();

        if (value == null)
        {
            Warn("Please select an actuator state.");
            return false;
        }

        TelemetryPacket<bool> packet =
            new TelemetryPacket<bool>(
                sensor.DeviceMacAddress,
                sensor.DeploymentLocation,
                value.Value);

        var response =
            await _telemetryApi.SendActuatorAsync(packet);

        if (!response.IsSuccessStatusCode)
        {
            HandleTelemetryError(response);
            return false;
        }

        _view.UpdateCurrentReading(
            value.Value ? "ON" : "OFF",
            SensorCategory.Actuator);

        _chart.AddPoint(
            value.Value ? 1 : 0,
            SensorCategory.Actuator);

        return true;
    }

    public async Task SeedTelemetryAsync()
    {
        SensorProfile? sensor =
            _state.SelectedSensor;

        if (sensor == null)
        {
            Warn("Please select a registered sensor first.");
            return;
        }

        const int packetCount = 100;
        int successfulPackets = 0;

        try
        {
            _view.Log(
                $"[SEED START] Generating {packetCount} mock " +
                $"{sensor.Category} telemetry packets for " +
                $"{sensor.DeviceMacAddress}...");

            switch (sensor.Category)
            {
                case SensorCategory.Environmental:

                    foreach (var packet in
                             _mockService.GenerateEnvironmental(
                                 sensor.DeviceMacAddress,
                                 sensor.DeploymentLocation,
                                 packetCount))
                    {
                        var response =
                            await _telemetryApi
                                .SendEnvironmentalAsync(packet);

                        if (!response.IsSuccessStatusCode)
                            continue;

                        successfulPackets++;

                        _view.UpdateCurrentReading(
                            packet.SensorValue.ToString("F2"),
                            SensorCategory.Environmental);

                        _chart.AddPoint(
                            packet.SensorValue,
                            SensorCategory.Environmental);

                        _view.ApplyAnomaly(
                            _anomalyDetector
                                .EvaluateEnvironmental(
                                    packet.SensorValue));

                        CountPacket();
                    }

                    break;

                case SensorCategory.PowerConsumption:

                    foreach (var packet in
                             _mockService.GeneratePower(
                                 sensor.DeviceMacAddress,
                                 sensor.DeploymentLocation,
                                 packetCount))
                    {
                        var response =
                            await _telemetryApi
                                .SendPowerAsync(packet);

                        if (!response.IsSuccessStatusCode)
                            continue;

                        successfulPackets++;

                        _view.UpdateCurrentReading(
                            packet.SensorValue.ToString(),
                            SensorCategory.PowerConsumption);

                        _chart.AddPoint(
                            packet.SensorValue,
                            SensorCategory.PowerConsumption);

                        _view.ApplyAnomaly(
                            _anomalyDetector
                                .EvaluatePower(
                                    packet.SensorValue));

                        CountPacket();
                    }

                    break;

                case SensorCategory.Actuator:

                    foreach (var packet in
                             _mockService.GenerateActuator(
                                 sensor.DeviceMacAddress,
                                 sensor.DeploymentLocation,
                                 packetCount))
                    {
                        var response =
                            await _telemetryApi
                                .SendActuatorAsync(packet);

                        if (!response.IsSuccessStatusCode)
                            continue;

                        successfulPackets++;

                        _view.UpdateCurrentReading(
                            packet.SensorValue ? "ON" : "OFF",
                            SensorCategory.Actuator);

                        _chart.AddPoint(
                            packet.SensorValue ? 1 : 0,
                            SensorCategory.Actuator);

                        CountPacket();
                    }

                    break;
            }

            _view.Log(
                $"[SEED COMPLETE] Successfully processed " +
                $"{successfulPackets} mock telemetry packets.");

            System.Windows.MessageBox.Show(
                $"Mock telemetry seeding completed.\n\n" +
                $"Successful packets: {successfulPackets}",
                "Smart-X",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch
        {
            _view.Log(
                "[SEED ERROR] Mock telemetry generation failed.");

            System.Windows.MessageBox.Show(
                "Mock telemetry seeding failed. Please verify that the Smart-X API is running.",
                "Mock Telemetry Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public async Task AttachFileAsync()
    {
        SensorProfile? sensor =
            _state.SelectedSensor;

        if (sensor == null)
        {
            Warn("Please select a registered sensor first.");
            return;
        }

        OpenFileDialog dialog =
            new OpenFileDialog
            {
                Filter =
                    "Config & Log Files (*.json;*.txt;*.log;*.png;*.jpg)|*.json;*.txt;*.log;*.png;*.jpg|" +
                    "All Files (*.*)|*.*",

                Title =
                    "Select Sensor Profile Attachment"
            };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var response =
                await _attachmentService.UploadAsync(
                    sensor.DeviceMacAddress,
                    dialog.FileName);

            if (!response.IsSuccessStatusCode)
            {
                _view.Log(
                    $"[ATTACHMENT ERROR] HTTP {response.StatusCode}");

                return;
            }

            long sizeKb =
                new System.IO.FileInfo(
                    dialog.FileName).Length / 1024;

            string fileName =
                System.IO.Path.GetFileName(
                    dialog.FileName);

            _view.SetAttachment(
                fileName,
                sizeKb);

            _view.Log(
                $"[ATTACHMENT UPLOADED] " +
                $"{fileName} → Sensor " +
                $"{sensor.DeviceMacAddress}");

            System.Windows.MessageBox.Show(
                "File successfully attached to the sensor profile.",
                "Smart-X",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch
        {
            _view.Log(
                "[ATTACHMENT ERROR] Could not upload attachment.");

            System.Windows.MessageBox.Show(
                "Could not upload the attachment.",
                "Attachment Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public async Task FetchTelemetryAsync()
    {
        try
        {
            var data =
                await _telemetryApi.GetEnvironmentalAsync();

            foreach (float value in data)
            {
                _chart.AddPoint(
                    value,
                    SensorCategory.Environmental);
            }

            _view.Log(
                $"[RETRIEVED] Fetched {data.Count} optimized " +
                $"historical records from gateway API.");
        }
        catch
        {
            _view.Log(
                "[FETCH ERROR] Failed to contact gateway.");
        }
    }

    public void SimulateAggregation()
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

        TelemetryPacket<float> result =
            meterA + meterB;

        _view.Log(
            $"[OPERATOR OVERLOAD +] Aggregated Load: " +
            $"{meterA.SensorValue} + " +
            $"{meterB.SensorValue} = " +
            $"{result.SensorValue} " +
            $"(ID: {result.DeviceMacAddress})");
    }

    private void CountPacket()
    {
        _state.IncrementPacketCount();

        _view.IncrementPacketCount(
            _state.PacketCount);
    }

    private void HandleTelemetryError(
      System.Net.Http.HttpResponseMessage response)
    {
        _view.Log(
            $"[API ERROR] HTTP {response.StatusCode} received from gateway.");
    }

    private void Warn(string message)
    {
        System.Windows.MessageBox.Show(
            message,
            "Smart-X",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Warning);
    }
    private async Task LoadHistoricalTelemetryAsync()
    {
        try
        {
            List<float> environmental =
                await _telemetryApi.GetEnvironmentalAsync();

            foreach (float value in environmental)
            {
                _chart.AddPoint(
                    value,
                    SensorCategory.Environmental);
            }

            List<int> power =
                await _telemetryApi.GetPowerAsync();

            foreach (int value in power)
            {
                _chart.AddPoint(
                    value,
                    SensorCategory.PowerConsumption);
            }

            List<bool> actuator =
                await _telemetryApi.GetActuatorAsync();

            foreach (bool value in actuator)
            {
                _chart.AddPoint(
                    value ? 1 : 0,
                    SensorCategory.Actuator);
            }

            _view.Log(
                $"[HISTORY] Loaded {environmental.Count} environmental, " +
                $"{power.Count} power and " +
                $"{actuator.Count} actuator telemetry records.");
        }
        catch
        {
            _view.Log(
                "[HISTORY ERROR] Failed to load historical telemetry.");
        }
    }
}