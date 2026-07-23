using HandballIntegration.Services;
using HandballManagerCore.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandballIntegration.Views
{
    public partial class UsersPage : Page
    {
        private readonly UsersApiService _usersService;
        private List<ManagedUserDto> _users = new List<ManagedUserDto>();

        public UsersPage()
        {
            InitializeComponent();

            _usersService = App.Services.GetRequiredService<UsersApiService>();
            Loaded += UsersPage_Loaded;
        }

        private async void UsersPage_Loaded(object sender, RoutedEventArgs e)
        {
            RoleComboBox.ItemsSource = Enum.GetNames(typeof(UserRole));
            RoleComboBox.SelectedItem = UserRole.Consultation.ToString();
            HideFormMessage();
            await LoadUsersAsync();
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private async void CreateUser_Click(object sender, RoutedEventArgs e)
        {
            await CreateUserAsync();
        }

        private async Task LoadUsersAsync()
        {
            SetBusyState(true);

            var result = await _usersService.GetUsersAsync();
            if (!result.Success)
            {
                _users = new List<ManagedUserDto>();
                UsersGrid.ItemsSource = _users;
                UsersHintText.Text = result.Message;
                UpdateMetrics();
                SetBusyState(false);
                return;
            }

            _users = result.Users
                .OrderBy(user => user.Username)
                .ToList();

            UsersGrid.ItemsSource = _users;
            UsersHintText.Text = $"{_users.Count} compte(s) visibles.";
            UpdateMetrics();
            SetBusyState(false);
        }

        private async Task CreateUserAsync()
        {
            HideFormMessage();

            var username = UsernameTextBox.Text.Trim();
            var email = EmailTextBox.Text.Trim();
            var role = RoleComboBox.SelectedItem?.ToString() ?? string.Empty;
            var password = PasswordInput.Password ?? string.Empty;
            var confirmPassword = ConfirmPasswordInput.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowFormMessage("Le nom d'utilisateur est obligatoire.", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                ShowFormMessage("Selectionne un role pour le nouveau compte.", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowFormMessage("Le mot de passe est obligatoire.", true);
                return;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                ShowFormMessage("La confirmation du mot de passe ne correspond pas.", true);
                return;
            }

            SetBusyState(true);

            var result = await _usersService.CreateUserAsync(new CreateManagedUserRequest
            {
                Username = username,
                Email = email,
                Password = password,
                Role = role
            });

            if (!result.Success)
            {
                ShowFormMessage(result.Message, true);
                SetBusyState(false);
                return;
            }

            ShowFormMessage(result.Message, false);
            ClearForm();
            await LoadUsersAsync();
            SetBusyState(false);
        }

        private void UpdateMetrics()
        {
            UsersCountText.Text = _users.Count.ToString();
            AdminsCountText.Text = _users.Count(user => string.Equals(user.Role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase)).ToString();
            ConsultationCountText.Text = _users.Count(user => string.Equals(user.Role, UserRole.Consultation.ToString(), StringComparison.OrdinalIgnoreCase)).ToString();
        }

        private void SetBusyState(bool isBusy)
        {
            RefreshUsersButton.IsEnabled = !isBusy;
            CreateUserButton.IsEnabled = !isBusy;
            UsernameTextBox.IsEnabled = !isBusy;
            EmailTextBox.IsEnabled = !isBusy;
            RoleComboBox.IsEnabled = !isBusy;
            PasswordInput.IsEnabled = !isBusy;
            ConfirmPasswordInput.IsEnabled = !isBusy;
        }

        private void ClearForm()
        {
            UsernameTextBox.Clear();
            EmailTextBox.Clear();
            PasswordInput.Clear();
            ConfirmPasswordInput.Clear();
            RoleComboBox.SelectedItem = UserRole.Consultation.ToString();
        }

        private void ShowFormMessage(string message, bool isError)
        {
            FormMessageBorder.Visibility = Visibility.Visible;
            FormMessageBorder.Background = TryFindResource(isError ? "DangerSoftBrush" : "AccentAltSoftBrush") as Brush;
            FormMessageText.Foreground = TryFindResource(isError ? "DangerBrush" : "AccentAltDarkBrush") as Brush;
            FormMessageText.Text = message;
        }

        private void HideFormMessage()
        {
            FormMessageText.Text = string.Empty;
            FormMessageBorder.Visibility = Visibility.Collapsed;
        }
    }
}
