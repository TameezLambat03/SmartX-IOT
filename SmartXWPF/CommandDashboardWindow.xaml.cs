using System.Windows;

namespace SmartXWPF;

public partial class CommandDashboardWindow : Window
{
    public CommandDashboardWindow()
    {
        InitializeComponent();
    }

    private void BtnMainMenu_Click(
        object sender,
        RoutedEventArgs e)
    {
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
        this.Close();
    }
}