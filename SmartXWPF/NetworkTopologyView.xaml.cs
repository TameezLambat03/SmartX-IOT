using System;
using System.Windows;
using System.Windows.Controls;

namespace SmartXWPF;

public partial class NetworkTopologyView : UserControl
{
    public NetworkTopologyView()
    {
        InitializeComponent();

        DpStartDate.SelectedDate = DateTime.Today;
        DpEndDate.SelectedDate = DateTime.Today;

        BtnFindRoute.IsEnabled = false;
        BtnQueryHistory.IsEnabled = false;
    }

    private void BtnFindRoute_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBox.Show(
            "Network routing will be connected in the next implementation step.",
            "Smart-X",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnQueryHistory_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBox.Show(
            "Historical telemetry queries will be connected to the AVL tree in the next implementation step.",
            "Smart-X",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}