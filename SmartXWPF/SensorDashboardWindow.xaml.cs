using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using SmartXWPF.Dashboard;
using SmartXWPF.Services;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace SmartXWPF;

public partial class SensorDashboardWindow : Window
{
    private readonly DashboardController _controller;

    public SensorDashboardWindow()
    {
        //create api service//request happens in ApiService.cs
        InitializeComponent();

        string? apiBaseUrl =
            ConfigurationManager.AppSettings["ApiBaseUrl"];

        ApiService apiService =
            new ApiService(apiBaseUrl!);

        DashboardView view =
            new DashboardView(this);

        TelemetryChartService chart =
            new TelemetryChartService(
                TelemetryChartCanvas);

        _controller =
            new DashboardController(
                view,
                apiService,
                chart);

        Loaded +=
            SensorDashboardWindow_Loaded;
    }

    private async void SensorDashboardWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await _controller.InitializeAsync();
    }

    private async void BtnRefreshSensors_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _controller.RefreshSensorsAsync();
    }

    private void LstSensors_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        _controller.SelectSensor(
            LstSensors.SelectedItem as SensorProfile);
    }

    private void CmbCategory_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (_controller == null)
        {
            return;
        }

        _controller.UpdateCategoryControls(
            CmbCategory.SelectedIndex);
    }

    private async void BtnRegisterSensor_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _controller.RegisterSensorAsync();
    }

    private async void BtnSubmitTelemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _controller.SubmitTelemetryAsync();
    }

    private async void BtnSeedTelemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _controller.SeedTelemetryAsync();
    }

    private async void BtnAttachFile_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _controller.AttachFileAsync();
    }

    private async void BtnFetchTelemetry_Click(
        object sender,
        RoutedEventArgs e)
    {
        await _controller.FetchTelemetryAsync();
    }

    private void BtnSimulateAggregation_Click(
        object sender,
        RoutedEventArgs e)
    {
        _controller.SimulateAggregation();
    }
    private void BtnEnvironmentalGraph_Click(
    object sender,
    RoutedEventArgs e)
    {
        _controller.ShowGraph(SensorCategory.Environmental);
    }

    private void BtnPowerGraph_Click(
        object sender,
        RoutedEventArgs e)
    {
        _controller.ShowGraph(SensorCategory.PowerConsumption);
    }

    private void BtnActuatorGraph_Click(
        object sender,
        RoutedEventArgs e)
    {
        _controller.ShowGraph(SensorCategory.Actuator);
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