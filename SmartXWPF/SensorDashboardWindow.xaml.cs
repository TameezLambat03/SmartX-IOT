using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Configuration;
using SmartX.Shared.Models;

namespace SmartXWPF;

public class DeploymentNode
{
    public string Name { get; set; }
    public DeploymentNode? Child { get; set; }

    public DeploymentNode(string name, DeploymentNode? child = null)
    {
        Name = name;
        Child = child;
    }
}

public partial class SensorDashboardWindow : Window
{
    private readonly HttpClient _httpClient = new HttpClient();
    private string _attachedFilePath = string.Empty;

    public SensorDashboardWindow()
    {
        InitializeComponent();
        _httpClient.BaseAddress = new Uri(ConfigurationManager.AppSettings["ApiBaseUrl"]);
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
            LblAttachedFile.Text = $"Attached: {Path.GetFileName(_attachedFilePath)} ({new FileInfo(_attachedFilePath).Length / 1024} KB)";
            LblAttachedFile.Foreground = new SolidColorBrush(Colors.LightGreen);
            LogTelemetryFeed($"Attached profile artifact: {Path.GetFileName(_attachedFilePath)}");
        }
    }

    private async void BtnSubmitTelemetry_Click(object sender, RoutedEventArgs e)
    {
        string mac = TxtMacAddress.Text.Trim();
        string deploymentRaw = TxtDeploymentPath.Text.Trim();
        string rawValue = TxtSensorValue.Text.Trim();

        DeploymentNode deploymentTree = BuildDeploymentHierarchy(deploymentRaw);
        if (!ValidateDeploymentRecursively(deploymentTree, 0))
        {
            LblStatusIndicator.Text = "STATUS: PATH INVALID";
            LblStatusIndicator.Foreground = new SolidColorBrush(Colors.OrangeRed);
            LogTelemetryFeed($"[REJECTED] Recursive validation failed for hierarchy: {deploymentRaw}");
            return;
        }

        try
        {
            HttpResponseMessage response = null;

            // Switch based on the UI dropdown category
            if (CmbCategory.SelectedIndex == 0) // Environmental (Float)
            {
                if (!float.TryParse(rawValue, out float floatVal))
                {
                    MessageBox.Show("Please enter a valid decimal for Environmental data.", "Format Error");
                    return;
                }
                var packet = new TelemetryPacket<float>(mac, deploymentRaw, floatVal);
                EvaluateVisualAnomaly(floatVal);
                response = await _httpClient.PostAsJsonAsync("/api/telemetry/environmental", packet);
            }
            else if (CmbCategory.SelectedIndex == 1) // Power Consumption (Int)
            {
                if (!int.TryParse(rawValue, out int intVal))
                {
                    MessageBox.Show("Please enter a whole number for Power Consumption.", "Format Error");
                    return;
                }
                var packet = new TelemetryPacket<int>(mac, deploymentRaw, intVal);
                response = await _httpClient.PostAsJsonAsync("/api/telemetry/power", packet);
            }
            else if (CmbCategory.SelectedIndex == 2) // Actuator (Bool)
            {
                if (!bool.TryParse(rawValue, out bool boolVal))
                {
                    MessageBox.Show("Please enter 'true' or 'false' for Actuator data.", "Format Error");
                    return;
                }
                var packet = new TelemetryPacket<bool>(mac, deploymentRaw, boolVal);
                response = await _httpClient.PostAsJsonAsync("/api/telemetry/actuator", packet);
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                LogTelemetryFeed($"[INGESTED] MAC: {mac} | Value: {rawValue} | Type: {CmbCategory.Text.Split(' ')[0]}");
            }
            else
            {
                LogTelemetryFeed($"[API ERROR] HTTP {response?.StatusCode} received from gateway.");
            }
        }
        catch (Exception ex)
        {
            LogTelemetryFeed($"[CONNECTION ERROR] Failed to push packet: {ex.Message}");
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

    private DeploymentNode BuildDeploymentHierarchy(string path)
    {
        string[] segments = path.Split(new[] { '/', '\\', '>' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return new DeploymentNode("Root");

        DeploymentNode root = new DeploymentNode(segments[0].Trim());
        DeploymentNode current = root;

        for (int i = 1; i < segments.Length; i++)
        {
            var nextNode = new DeploymentNode(segments[i].Trim());
            current.Child = nextNode;
            current = nextNode;
        }

        return root;
    }

    private bool ValidateDeploymentRecursively(DeploymentNode? node, int depth)
    {
        if (node == null)
        {
            return depth >= 2;
        }

        if (string.IsNullOrWhiteSpace(node.Name))
        {
            return false;
        }

        return ValidateDeploymentRecursively(node.Child, depth + 1);
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