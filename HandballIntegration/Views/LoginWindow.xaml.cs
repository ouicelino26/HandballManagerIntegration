using HandballIntegration.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace HandballIntegration.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IApiAuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();

            _authService = App.Services.GetRequiredService<IApiAuthService>();
            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Focus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteLoginAsync();
        }

        private async Task ExecuteLoginAsync()
        {
            SetBusyState(true);
            HideError();

            try
            {
                var username = UsernameTextBox.Text?.Trim() ?? string.Empty;
                var password = PasswordInput.Password ?? string.Empty;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    ShowError("Renseigne un nom d'utilisateur et un mot de passe.");
                    return;
                }

                var result = await _authService.LoginAsync(username, password);
                if (!result.Success)
                {
                    ShowError(result.Message);
                    PasswordInput.Clear();
                    PasswordInput.Focus();
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Connexion impossible : {ex.Message}");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void SetBusyState(bool isBusy)
        {
            UsernameTextBox.IsEnabled = !isBusy;
            PasswordInput.IsEnabled = !isBusy;
            LoginButton.IsEnabled = !isBusy;
            BusyText.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorContainer.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Text = string.Empty;
            ErrorContainer.Visibility = Visibility.Collapsed;
        }
    }
}
