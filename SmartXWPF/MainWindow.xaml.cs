
using System.Windows;

namespace SmartXWPF
{
    public partial class MainWindow : Window
    {
        private readonly SensorDashboardWindow _telemetryView;
        private readonly CommandDashboardWindow _commandView;
        private readonly NetworkTopologyView _networkTopologyView;

        public MainWindow()
        {
            InitializeComponent();

            _telemetryView = new SensorDashboardWindow();
            _commandView = new CommandDashboardWindow();
            _networkTopologyView = new NetworkTopologyView();

            ShowTelemetryView();
        }

        private void ShowTelemetryView()
        {
            MainContent.Content = _telemetryView;
        }

        private void ShowCommandView()
        {
            MainContent.Content = _commandView;
        }

        private void BtnIngestion_Click(object sender, RoutedEventArgs e)
        {
            ShowTelemetryView();
        }

        private void BtnCommandDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowCommandView();
        }
        private void BtnNetworkTopology_Click(
    object sender,
    RoutedEventArgs e)
        {
            MainContent.Content = _networkTopologyView;
        }
    }
}
