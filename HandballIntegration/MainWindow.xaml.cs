using HandballIntegration.Services;
using HandballIntegration.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HandballIntegration
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly IApiAuthService _authService;
        private bool _navigationReady;
        private bool _isSidebarCollapsed;

        public MainWindow()
        {
            InitializeComponent();

            _apiService = App.Services.GetRequiredService<ApiService>();
            _authService = App.Services.GetRequiredService<IApiAuthService>();
            DataContext = new MainViewModel();
            Loaded += MainWindow_Loaded;
        }

        private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_navigationReady || ContentFrame is null)
            {
                return;
            }

            if (NavigationList.SelectedItem is not ListBoxItem item)
            {
                return;
            }

            NavigateTo(item.Tag?.ToString());
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplySidebarState();
                _navigationReady = true;

                if (NavigationList.SelectedItem is null)
                {
                    NavigationList.SelectedIndex = 0;
                }
                else if (NavigationList.SelectedItem is ListBoxItem item)
                {
                    NavigateTo(item.Tag?.ToString());
                }

                await RefreshShellStateAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    "Erreur pendant le chargement de la session : " + ex.Message,
                    "Chargement",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
            ApplySidebarState();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _authService.Logout();
            Hide();

            var loginWindow = new Views.LoginWindow();
            var loginResult = loginWindow.ShowDialog();

            if (loginResult != true)
            {
                Close();
                return;
            }

            Show();
            Activate();
            await RefreshShellStateAsync();

            if (NavigationList.SelectedItem is ListBoxItem item)
            {
                NavigateTo(item.Tag?.ToString());
            }
        }

        private void ApplySidebarState()
        {
            SidebarColumn.Width = new GridLength(_isSidebarCollapsed ? 92 : 272);
            SidebarGapColumn.Width = new GridLength(_isSidebarCollapsed ? 12 : 18);
            SidebarBorder.Padding = _isSidebarCollapsed ? new Thickness(12) : new Thickness(18);
            SidebarStatusCard.Width = _isSidebarCollapsed ? 40 : double.NaN;
            SidebarStatusCard.Padding = _isSidebarCollapsed ? new Thickness(0) : new Thickness(14, 12, 14, 12);
            SidebarStatusCard.HorizontalAlignment = _isSidebarCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
            SidebarToggleGlyph.Text = _isSidebarCollapsed ? ">" : "<";

            var expandedVisibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;

            SidebarBrandTextPanel.Visibility = expandedVisibility;
            SidebarStatusTextPanel.Visibility = expandedVisibility;
            DashboardNavTextPanel.Visibility = expandedVisibility;
            IntegrationNavTextPanel.Visibility = expandedVisibility;
            PlayersNavTextPanel.Visibility = expandedVisibility;
            PdfNavTextPanel.Visibility = expandedVisibility;
            AccountsNavTextPanel.Visibility = expandedVisibility;
            LogoutTextPanel.Visibility = expandedVisibility;
        }

        private void NavigateTo(string? tag)
        {
            if (ContentFrame is null || ShellSectionText is null || ShellTitleText is null || ShellSubtitleText is null)
            {
                return;
            }

            switch (tag)
            {
                case "dashboard":
                    ContentFrame.Navigate(new Views.DashboardPage());
                    SetShellHeader(
                        "Vue rapide",
                        "Dashboard",
                        "Les informations utiles, sans surcharge.");
                    break;

                case "integration":
                    ContentFrame.Navigate(new Views.IntegrationPage());
                    SetShellHeader(
                        "Import",
                        "Integration",
                        "Choisir, verifier puis integrer.");
                    break;

                case "players":
                    ContentFrame.Navigate(new Views.PlayersPage());
                    SetShellHeader(
                        "Reference",
                        "Joueuses",
                        "Filtrer vite et agir sans bruit visuel.");
                    break;

                case "pdf":
                    ContentFrame.Navigate(new Views.SendPdf());
                    SetShellHeader(
                        "Export",
                        "Studio PDF",
                        "Composer la page sans perdre le focus.");
                    break;

                case "accounts":
                    ContentFrame.Navigate(new Views.UsersPage());
                    SetShellHeader(
                        "Administration",
                        "Comptes",
                        "Creer un compte et verifier les acces en un coup d'oeil.");
                    break;
            }
        }

        private async Task RefreshShellStateAsync()
        {
            bool success = await _apiService.TestConnectionAsync();

            if (DataContext is MainViewModel vm)
            {
                vm.IsApiConnected = success;
                vm.CurrentUsername = _apiService.CurrentUsername ?? "Session";
                vm.CurrentRole = _apiService.CurrentRole ?? "Admin";
            }
        }

        private void SetShellHeader(string section, string title, string subtitle)
        {
            ShellSectionText.Text = section;
            ShellTitleText.Text = title;
            ShellSubtitleText.Text = subtitle;
        }
    }
}
