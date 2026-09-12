using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using SmartXWPF.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartXWPF.Dashboard;

public class DashboardView
{
    private readonly SensorDashboardWindow _window;

    public DashboardView(SensorDashboardWindow window)
    {
        _window = window;
    }

    private TextBlock Text(string name) =>
        (TextBlock)_window.FindName(name);

    private ComboBox Combo(string name) =>
        (ComboBox)_window.FindName(name);

    private TextBox Input(string name) =>
        (TextBox)_window.FindName(name);

    private ListBox List(string name) =>
        (ListBox)_window.FindName(name);

    public void SetSensors(
        List<SensorProfile> sensors)
    {
        List("LstSensors").ItemsSource = sensors;

        Text("LblDeviceCount").Text =
            sensors.Count.ToString();

        Text("LblSensorCount").Text =
            $"{sensors.Count} registered sensor(s)";
    }

    public void SetGatewayStatus(bool online)
    {
        TextBlock gateway =
            Text("LblGatewayStatus");

        TextBlock system =
            Text("LblSystemStatus");

        if (online)
        {
            gateway.Text = "GATEWAY ONLINE";
            gateway.Foreground =
                new SolidColorBrush(Colors.LightGreen);

            system.Text = "NORMAL";
            system.Foreground =
                new SolidColorBrush(Colors.LightGreen);
        }
        else
        {
            gateway.Text = "GATEWAY OFFLINE";
            gateway.Foreground =
                new SolidColorBrush(Colors.Red);

            system.Text = "GATEWAY OFFLINE";
            system.Foreground =
                new SolidColorBrush(Colors.Red);
        }
    }

    public void DisplaySelectedSensor(
     SensorProfile sensor)
    {
        Text("LblSelectedSensorMac").Text =
            sensor.DeviceMacAddress;

        Text("LblSelectedSensorCategory").Text =
            GetCategoryDisplayName(sensor.Category);

        Text("LblSelectedSensorLocation").Text =
            sensor.DeploymentLocation;

        Text("LblAttachedFile").Text =
     string.IsNullOrWhiteSpace(sensor.AttachmentFileName)
         ? "No attachment"
         : $"Attached to selected sensor: {sensor.AttachmentFileName}";

        ComboBox category =
            Combo("CmbCategory");

        
    

    TextBox value =
            Input("TxtSensorValue");

        ComboBox actuator =
            Combo("CmbActuatorState");

        switch (sensor.Category)
        {
            case SensorCategory.Environmental:
                category.SelectedIndex = 0;
                value.Visibility = Visibility.Visible;
                actuator.Visibility = Visibility.Collapsed;
                value.Text = "45.5";
                break;

            case SensorCategory.PowerConsumption:
                category.SelectedIndex = 1;
                value.Visibility = Visibility.Visible;
                actuator.Visibility = Visibility.Collapsed;
                value.Text = "742";
                break;

            case SensorCategory.Actuator:
                category.SelectedIndex = 2;
                value.Visibility = Visibility.Collapsed;
                actuator.Visibility = Visibility.Visible;
                break;
        }
    }

    public void UpdateCategoryControls(
        int selectedIndex)
    {
        TextBox value =
            Input("TxtSensorValue");

        ComboBox actuator =
            Combo("CmbActuatorState");

        bool isActuator =
            selectedIndex == 2;

        value.Visibility =
            isActuator
                ? Visibility.Collapsed
                : Visibility.Visible;

        actuator.Visibility =
            isActuator
                ? Visibility.Visible
                : Visibility.Collapsed;

        if (selectedIndex == 0)
        {
            value.Text = "45.5";
        }
        else if (selectedIndex == 1)
        {
            value.Text = "742";
        }
    }

    public string GetMac() =>
        Input("TxtMacAddress").Text.Trim();

    public string GetDeployment() =>
        Input("TxtDeploymentPath").Text.Trim();

    public string GetTelemetryValue() =>
        Input("TxtSensorValue").Text.Trim();

    public SensorCategory? GetSelectedCategory()
    {
        return Combo("CmbCategory").SelectedIndex switch
        {
            0 => SensorCategory.Environmental,
            1 => SensorCategory.PowerConsumption,
            2 => SensorCategory.Actuator,
            _ => null
        };
    }

    public bool? GetActuatorValue()
    {
        if (Combo("CmbActuatorState").SelectedItem
            is not ComboBoxItem item)
        {
            return null;
        }

        string state =
            item.Content?.ToString() ?? string.Empty;

        return state.Equals(
            "ON",
            System.StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateCurrentReading(
        string value,
        SensorCategory category)
    {
        Text("LblCurrentReading").Text = value;

        Text("LblReadingCategory").Text =
            $"{GetCategoryDisplayName(category)} telemetry";

        Text("LblLastUpdate").Text =
            System.DateTime.Now.ToString("HH:mm:ss");
    }

    public void ApplyAnomaly(
        AnomalyResult result)
    {
        TextBlock status =
            Text("LblStatusIndicator");

        TextBlock alert =
            Text("LblAnomalyAlert");

        if (result.IsAnomalous)
        {
            status.Foreground =
                new SolidColorBrush(Colors.Red);

            alert.Foreground =
                new SolidColorBrush(Colors.OrangeRed);
        }
        else
        {
            status.Foreground =
                new SolidColorBrush(Colors.LightGreen);

            alert.Foreground =
                new SolidColorBrush(Colors.LightGray);
        }

        status.Text = result.Status;
        alert.Text = result.Message;
    }

    public void IncrementPacketCount(
        int count)
    {
        Text("LblPacketCount").Text =
            count.ToString();
    }

    public void SetAttachment(
        string fileName,
        long sizeKb)
    {
        TextBlock label =
            Text("LblAttachedFile");

        label.Text =
            $"Attached: {fileName} ({sizeKb} KB)";

        label.Foreground =
            new SolidColorBrush(
                Colors.LightGreen);
    }

    public void Log(string message)
    {
        TextBlock feed =
            Text("TxtTelemetryFeed");

        feed.Text +=
            $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
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

            _ => "Unknown"
        };
    }
}