using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;
using HandballIntegration.Data;
using HandballIntegration.Services;

namespace HandballIntegration.Presentation.ViewModels;

public sealed class AdminShellViewModel : ObservableObject, IDisposable
{
    private readonly IAdminCapabilitiesService _capabilities;
    private readonly IAdminNavigationService _navigation;
    private readonly IAdminSessionService _session;
    private readonly IApiAuthService _auth;
    private readonly IAdminModuleFactory _moduleFactory;
    private IReadOnlyList<AdminNavigationItem> _allItems = [];
    private AdminNavigationItem? _selectedNavigation;
    private IAdminModuleViewModel? _currentModule;
    private bool _isSidebarCollapsed;
    private bool _isLoading;
    private string _apiStatus = "Verification";
    private string _apiVersion = "Indisponible";
    private string _globalSearch = string.Empty;
    private string _notice = "Chargement des autorisations serveur.";

    public AdminShellViewModel(
        IAdminCapabilitiesService capabilities,
        IAdminNavigationService navigation,
        IAdminSessionService session,
        IApiAuthService auth,
        IAdminModuleFactory moduleFactory,
        ApiSettings settings)
    {
        _capabilities = capabilities;
        _navigation = navigation;
        _session = session;
        _auth = auth;
        _moduleFactory = moduleFactory;
        ApplicationName = settings.ApplicationName;
        Environment = settings.EnvironmentLabel;
        ClientVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "indisponible";
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
        RefreshCapabilitiesCommand = new AsyncRelayCommand(InitializeAsync, () => !IsLoading);
        LogoutCommand = new RelayCommand(Logout);
    }

    public event EventHandler? LogoutRequested;
    public ObservableCollection<AdminNavigationItem> NavigationItems { get; } = [];
    public IRelayCommand ToggleSidebarCommand { get; }
    public IAsyncRelayCommand RefreshCapabilitiesCommand { get; }
    public IRelayCommand LogoutCommand { get; }
    public string ApplicationName { get; }
    public string Environment { get; }
    public string ClientVersion { get; }
    public string Username => _session.Current?.Username ?? "Session indisponible";
    public string Role => _session.Current?.Role ?? "Non authentifie";
    public string UserInitial => string.IsNullOrWhiteSpace(Username) ? "?" : Username[..1].ToUpperInvariant();
    public string PageTitle => CurrentModule?.Title ?? "Administration";
    public string PageSubtitle => CurrentModule?.Subtitle ?? "Selectionnez un module autorise.";
    public string Breadcrumb => SelectedNavigation is null ? "Administration" : $"Administration / {SelectedNavigation.Label}";

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set => SetProperty(ref _isSidebarCollapsed, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            SetProperty(ref _isLoading, value);
            RefreshCapabilitiesCommand.NotifyCanExecuteChanged();
        }
    }

    public string ApiStatus { get => _apiStatus; private set => SetProperty(ref _apiStatus, value); }
    public string ApiVersion { get => _apiVersion; private set => SetProperty(ref _apiVersion, value); }
    public string Notice { get => _notice; private set => SetProperty(ref _notice, value); }

    public string GlobalSearch
    {
        get => _globalSearch;
        set
        {
            if (SetProperty(ref _globalSearch, value))
            {
                FilterNavigation();
            }
        }
    }

    public AdminNavigationItem? SelectedNavigation
    {
        get => _selectedNavigation;
        set
        {
            if (!SetProperty(ref _selectedNavigation, value) || value is null)
            {
                return;
            }

            OnPropertyChanged(nameof(Breadcrumb));
            _ = NavigateAsync(value);
        }
    }

    public IAdminModuleViewModel? CurrentModule
    {
        get => _currentModule;
        private set
        {
            if (_currentModule is IDisposable disposable)
            {
                disposable.Dispose();
            }

            SetProperty(ref _currentModule, value);
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageSubtitle));
        }
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        NavigationItems.Clear();
        try
        {
            await _capabilities.RefreshAsync();
            _allItems = _navigation.Build(_capabilities.Current);
            FilterNavigation();
            ApiStatus = "Disponible";
            ApiVersion = _capabilities.ApiVersion;
            Notice = $"{_allItems.Count} module(s) autorise(s) par le serveur.";
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(UserInitial));
            SelectedNavigation = _allItems.FirstOrDefault(item => item.Tag == "dashboard") ?? _allItems.FirstOrDefault();
        }
        catch (AdminApiException exception)
        {
            ApiStatus = exception.Error.Status == System.Net.HttpStatusCode.Forbidden
                ? "Acces refuse"
                : "Indisponible";
            ApiVersion = "Indisponible";
            Notice = $"{exception.Error.Message} {exception.Error.Action}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task NavigateAsync(AdminNavigationItem item)
    {
        if (!_navigation.CanAccess(item.Tag, _capabilities.Current))
        {
            Notice = "Ce module n'est plus autorise. Les capabilities vont etre rechargees.";
            await InitializeAsync();
            return;
        }

        var module = _moduleFactory.Create(item.Tag);
        CurrentModule = module;
        Notice = item.Status == AdminModuleStatus.BlockedByApi
            ? "Module bloque par un contrat API manquant. Aucun contenu fictif n'est affiche."
            : item.Description;
        await module.InitializeAsync();
    }

    private void FilterNavigation()
    {
        var filter = GlobalSearch.Trim();
        NavigationItems.Clear();
        foreach (var item in _allItems.Where(item =>
                     string.IsNullOrWhiteSpace(filter) ||
                     item.Label.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                     item.Description.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
        {
            NavigationItems.Add(item);
        }
    }

    private void Logout()
    {
        _auth.Logout();
        NavigationItems.Clear();
        CurrentModule = null;
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (CurrentModule is IDisposable disposable)
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
