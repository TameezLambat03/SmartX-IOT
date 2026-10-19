
using System;
using System.Windows;
using SmartXWPF.Services;

namespace SmartXWPF
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;
        private bool _isInitialized;

        public LoginWindow()
        {
            InitializeComponent();

            _authService = new AuthService(
                "https://mxcsgtlgyzyazkgcenns.supabase.co",
                "sb_publishable_knn4chEergUMB_p9bw_o0A_TvMyg42w");

            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _authService.InitializeAsync();
                _isInitialized = true;
            }
            catch
            {
                StatusTextBlock.Text =
                    "Could not connect to the authentication service. Check your Supabase configuration and internet connection.";
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                StatusTextBlock.Text = "Authentication service is not ready.";
                return;
            }

            string email = EmailTextBox.Text.Trim();
            string password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                StatusTextBlock.Text = "Enter your email and password.";
                return;
            }

            try
            {
                bool success = await _authService.LoginAsync(email, password);

                if (success)
                {
                    var mainWindow = new MainWindow();
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    Close();
                }
                else
                {
                    StatusTextBlock.Text =
                        "Login failed. Check your credentials and try again.";
                }
            }
            catch
            {
                StatusTextBlock.Text = "An error occurred while logging in.";
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                StatusTextBlock.Text = "Authentication service is not ready.";
                return;
            }

            string email = EmailTextBox.Text.Trim();
            string password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                StatusTextBlock.Text =
                    "Enter an email and password to register.";
                return;
            }

            if (password.Length < 6)
            {
                StatusTextBlock.Text =
                    "Your password must be at least 6 characters.";
                return;
            }

            try
            {
                bool success = await _authService.RegisterAsync(email, password);

                StatusTextBlock.Text = success
                    ? "Registration request submitted. Check your email if confirmation is required, then log in."
                    : "Registration failed. Check your details or whether the account already exists.";
            }
            catch
            {
                StatusTextBlock.Text = "An error occurred while registering.";
            }
        }
    }
}
