using System.Reflection;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;
using HandballIntegration.Data;
using HandballIntegration.Services;
using HandballIntegration.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HandballIntegration;

public partial class MainWindow : Window
{
    private readonly IApiAuthService _authService;
    private readonly IAdminCapabilitiesService _capabilitiesService;
    private readonly IAdminNavigationService _navigationService;
    private readonly IAdminSessionService _sessionService;
    private readonly ApiSettings _settings;
    private IReadOnlyList<AdminNavigationItem> _modules = [];
    private bool _navigationReady;
    private bool _isSidebarCollapsed;

    public MainWindow()
    {
        InitializeComponent();

        _authService = App.Services.GetRequiredService<IApiAuthService>();
        _capabilitiesService = App.Services.GetRequiredService<IAdminCapabilitiesService>();
        _navigationService = App.Services.GetRequiredService<IAdminNavigationService>();
        _sessionService = App.Services.GetRequiredService<IAdminSessionService>();
        _settings = App.Services.GetRequiredService<IOptions<ApiSettings>>().Value;

        ApplicationNameText.Text = _settings.ApplicationName;
        SidebarEnvironmentText.Text = _settings.EnvironmentLabel;
        ClientVersionText.Text = $"Client {ReadClientVersion()}";
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySidebarState();
        await RefreshShellAsync();
    }

    private async Task RefreshShellAsync(string? preferredTag = null)
    {
        _navigationReady = false;
        NavigationList.ItemsSource = null;
        SetApiState(false, "Verification API");
        UpdateIdentity();

        try
        {
            await _capabilitiesService.RefreshAsync();
            _modules = _navigationService.Build(_capabilitiesService.Current);
            NavigationList.ItemsSource = _modules;
            ApiVersionText.Text = $"API {_capabilitiesService.ApiVersion}";
            SetApiState(true, "API disponible");
            ShellNoticeText.Text = $"{_modules.Count} module(s) autorise(s) par l'API";
            _navigationReady = true;

            var target = _modules.FirstOrDefault(item => item.Tag == preferredTag) ?? _modules.FirstOrDefault();
            if (target is null)
            {
                ShowUnavailableState(
                    "Aucun module autorise",
                    "L'API n'a retourne aucun module accessible pour cette session.",
                    AdminModuleStatus.Blocked);
                return;
            }

            NavigationList.SelectedItem = target;
            NavigateTo(target);
        }
        catch (AdminSessionExpiredException)
        {
            ShowCapabilityFailure("Votre session a expire. Utilisez Deconnexion pour vous reconnecter.");
        }
        catch (AdminApiException exception)
        {
            ShowCapabilityFailure($"{exception.Error.Message} {exception.Error.Action}");
        }
        catch (HttpRequestException)
        {
            ShowCapabilityFailure("L'API d'administration est inaccessible. Verifiez le reseau puis reessayez.");
        }
        catch (TaskCanceledException)
        {
            ShowCapabilityFailure("L'API n'a pas repondu dans le delai prevu. Reessayez dans quelques instants.");
        }
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_navigationReady || NavigationList.SelectedItem is not AdminNavigationItem item)
        {
            return;
        }

        if (!_navigationService.CanAccess(item.Tag, _capabilitiesService.Current))
        {
            ShowUnavailableState(
                "Acces refuse",
                "Ce module n'est pas autorise par les capacites de la session courante.",
                AdminModuleStatus.Blocked);
            return;
        }

        NavigateTo(item);
    }

    private void NavigateTo(AdminNavigationItem item)
    {
        ContentFrame.Content = item.Tag switch
        {
            "dashboard" => new DashboardPage(),
            "integration" => new IntegrationPage(),
            "players" => new PlayersPage(),
            "users" => new UsersPage(),
            _ => new ModuleStatusPage(item)
        };

        ShellSectionText.Text = item.RequiredPermission.ToUpperInvariant();
        ShellTitleText.Text = item.Label;
        ShellSubtitleText.Text = item.Description;
        ShellNoticeText.Text = $"Etat du module : {item.Status}";
    }

    private void ShowCapabilityFailure(string message)
    {
        _modules = [];
        NavigationList.ItemsSource = _modules;
        ApiVersionText.Text = "API indisponible";
        SetApiState(false, "API indisponible");
        ShellNoticeText.Text = message;
        ShowUnavailableState(
            "Capacites indisponibles",
            "La navigation reste fermee tant que l'API n'a pas confirme les autorisations.",
            AdminModuleStatus.Blocked);
    }

    private void ShowUnavailableState(string title, string description, string status)
    {
        var item = new AdminNavigationItem(
            "unavailable",
            title,
            description,
            "--",
            "Capability.Required",
            status,
            false);
        ContentFrame.Content = new ModuleStatusPage(item);
        ShellSectionText.Text = "SECURITE";
        ShellTitleText.Text = title;
        ShellSubtitleText.Text = description;
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;
        ApplySidebarState();
    }

    private void ApplySidebarState()
    {
        SidebarColumn.Width = new GridLength(_isSidebarCollapsed ? 84 : 292);
        SidebarBorder.Padding = _isSidebarCollapsed ? new Thickness(12) : new Thickness(18);
        BrandTextPanel.Visibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarApiTextPanel.Visibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        LogoutText.Visibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarApiCard.HorizontalAlignment = _isSidebarCollapsed
            ? HorizontalAlignment.Center
            : HorizontalAlignment.Stretch;
        NavigationList.ItemTemplate = (DataTemplate)FindResource(
            _isSidebarCollapsed ? "CollapsedNavigationTemplate" : "ExpandedNavigationTemplate");
        SidebarToggleButton.Content = _isSidebarCollapsed ? ">" : "<";
        System.Windows.Automation.AutomationProperties.SetName(
            SidebarToggleButton,
            _isSidebarCollapsed ? "Deplier le menu" : "Replier le menu");
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _authService.Logout();
        Hide();

        var loginWindow = new LoginWindow();
        var loginResult = loginWindow.ShowDialog();
        if (loginResult != true)
        {
            Close();
            return;
        }

        Show();
        Activate();
        await RefreshShellAsync();
    }

    private void UpdateIdentity()
    {
        var session = _sessionService.Current;
        UsernameText.Text = session?.Username ?? "Session indisponible";
        RoleText.Text = session?.Role ?? "Non authentifie";
        UserInitialText.Text = string.IsNullOrWhiteSpace(session?.Username)
            ? "?"
            : session.Username[..1].ToUpperInvariant();
    }

    private void SetApiState(bool available, string label)
    {
        SidebarApiText.Text = label;
        SidebarApiDot.Fill = (Brush)FindResource(available ? "SuccessBrush" : "WarningBrush");
    }

    private static string ReadClientVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "indisponible";
}
