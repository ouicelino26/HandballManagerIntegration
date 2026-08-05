using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;
using HandballIntegration.Core.Abstractions;
using HandballIntegration.Core.Models;
using HandballManagerCore.DTO;

namespace HandballIntegration.Presentation.ViewModels;

public sealed class PlayersAdminViewModel : AdminPageViewModelBase
{
    private readonly IAdminPlayerApiClient _playerClient;
    private readonly IAdminTeamApiClient _teamClient;
    private readonly IAdminReferenceDataApiClient _referenceClient;
    private readonly IAdminCapabilitiesService _capabilities;
    private CancellationTokenSource? _searchSource;
    private PlayerListItemDto? _selectedPlayer;
    private string _search = string.Empty;
    private TeamDto? _filterTeam;
    private bool? _activeOnly = true;
    private int _page = 1;
    private int _pageSize = 25;
    private bool _isCreating;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private DateTime? _birthday;
    private int? _age;
    private TeamDto? _editTeam;
    private LookupItemDto? _editPosition;
    private LookupItemDto? _editNationality;
    private int? _number;
    private bool _isActive = true;

    public PlayersAdminViewModel(
        IAdminPlayerApiClient playerClient,
        IAdminTeamApiClient teamClient,
        IAdminReferenceDataApiClient referenceClient,
        IAdminCapabilitiesService capabilities) : base(
            "Joueuses",
            "Recherchez, consultez et maintenez les informations exposees par l'API.")
    {
        _playerClient = playerClient;
        _teamClient = teamClient;
        _referenceClient = referenceClient;
        _capabilities = capabilities;
        ApplyFiltersCommand = new AsyncRelayCommand(ApplyFiltersAsync, () => !IsLoading);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => !IsLoading);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => Players.Count == PageSize && !IsLoading);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => Page > 1 && !IsLoading);
        NewPlayerCommand = new RelayCommand(StartCreate, () => _capabilities.Has(AdminPermissionNames.PlayersCreate));
        SavePlayerCommand = new AsyncRelayCommand(SavePlayerAsync, CanSavePlayer);
        CancelEditCommand = new RelayCommand(CancelEdit);
    }

    public ObservableCollection<PlayerListItemDto> Players { get; } = [];
    public ObservableCollection<TeamDto> Teams { get; } = [];
    public ObservableCollection<LookupItemDto> Positions { get; } = [];
    public ObservableCollection<LookupItemDto> Nationalities { get; } = [];
    public IReadOnlyList<int> PageSizes { get; } = [25, 50, 100];
    public IAsyncRelayCommand ApplyFiltersCommand { get; }
    public IAsyncRelayCommand ClearFiltersCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public IRelayCommand NewPlayerCommand { get; }
    public IAsyncRelayCommand SavePlayerCommand { get; }
    public IRelayCommand CancelEditCommand { get; }

    public PlayerListItemDto? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (!SetProperty(ref _selectedPlayer, value) || value is null)
            {
                return;
            }

            IsCreating = false;
            FirstName = ExtractFirstName(value.FullName);
            LastName = ExtractLastName(value.FullName);
            Birthday = value.Birthday;
            Age = value.Age;
            EditTeam = Teams.FirstOrDefault(item => item.TeamId == value.TeamId);
            EditPosition = Positions.FirstOrDefault(item => item.Id == value.PositionId);
            EditNationality = Nationalities.FirstOrDefault(item =>
                string.Equals(item.Name, value.Nationality, StringComparison.OrdinalIgnoreCase));
            Number = value.Number;
            IsActive = value.IsActive;
            OnPropertyChanged(nameof(IsEditorVisible));
            SavePlayerCommand.NotifyCanExecuteChanged();
        }
    }

    public string Search
    {
        get => _search;
        set
        {
            if (SetProperty(ref _search, value))
            {
                ScheduleSearch();
            }
        }
    }

    public TeamDto? FilterTeam { get => _filterTeam; set => SetProperty(ref _filterTeam, value); }
    public bool? ActiveOnly { get => _activeOnly; set => SetProperty(ref _activeOnly, value); }

    public int Page
    {
        get => _page;
        private set
        {
            SetProperty(ref _page, value);
            PreviousPageCommand.NotifyCanExecuteChanged();
        }
    }

    public int PageSize { get => _pageSize; set => SetProperty(ref _pageSize, value); }
    public bool IsCreating { get => _isCreating; private set => SetProperty(ref _isCreating, value); }
    public string FirstName { get => _firstName; set { SetProperty(ref _firstName, value); SavePlayerCommand.NotifyCanExecuteChanged(); } }
    public string LastName { get => _lastName; set { SetProperty(ref _lastName, value); SavePlayerCommand.NotifyCanExecuteChanged(); } }
    public DateTime? Birthday { get => _birthday; set => SetProperty(ref _birthday, value); }
    public int? Age { get => _age; set => SetProperty(ref _age, value); }
    public TeamDto? EditTeam { get => _editTeam; set => SetProperty(ref _editTeam, value); }
    public LookupItemDto? EditPosition { get => _editPosition; set => SetProperty(ref _editPosition, value); }
    public LookupItemDto? EditNationality { get => _editNationality; set => SetProperty(ref _editNationality, value); }
    public int? Number { get => _number; set => SetProperty(ref _number, value); }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
    public bool IsEditorVisible => IsCreating || SelectedPlayer is not null;
    public string IdentityStatus => SelectedPlayer is null
        ? "Nouvelle identite"
        : "PARTIAL - le contrat legacy ne separe pas nom et prenom en lecture.";
    public string MergeStatus => "BLOCKED_BY_API - aucune route de fusion auditee n'est deployee.";

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var playersTask = _playerClient.GetPlayersAsync(
            Page,
            PageSize,
            Search,
            FilterTeam?.TeamId,
            ActiveOnly,
            cancellationToken);
        var teamsTask = _teamClient.GetTeamsAsync(cancellationToken);
        var positionsTask = _referenceClient.GetPositionsAsync(cancellationToken);
        var nationalitiesTask = _referenceClient.GetNationalitiesAsync(cancellationToken);
        var players = await playersTask;
        var teams = await teamsTask;
        var positions = await positionsTask;
        var nationalities = await nationalitiesTask;

        Replace(Players, players.Items);
        Replace(Teams, teams);
        Replace(Positions, positions);
        Replace(Nationalities, nationalities);
        NextPageCommand.NotifyCanExecuteChanged();
        return Players.Count == 0
            ? AdminPageState.Empty("Aucune joueuse ne correspond aux filtres.")
            : AdminPageState.Partial("Lecture paginee disponible. Audit, concurrence et identite forte restent bloques par l'API.");
    }

    private void StartCreate()
    {
        SelectedPlayer = null;
        IsCreating = true;
        FirstName = string.Empty;
        LastName = string.Empty;
        Birthday = null;
        Age = null;
        EditTeam = null;
        EditPosition = null;
        EditNationality = null;
        Number = null;
        IsActive = true;
        OnPropertyChanged(nameof(IsEditorVisible));
        OnPropertyChanged(nameof(IdentityStatus));
        SavePlayerCommand.NotifyCanExecuteChanged();
    }

    private bool CanSavePlayer()
    {
        if (IsLoading)
        {
            return false;
        }

        if (IsCreating)
        {
            return _capabilities.Has(AdminPermissionNames.PlayersCreate) &&
                   !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName);
        }

        return SelectedPlayer is not null &&
               _capabilities.Has(AdminPermissionNames.PlayersUpdate) &&
               !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName);
    }

    private async Task SavePlayerAsync()
    {
        if (!CanSavePlayer())
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            var mutation = new AdminPlayerMutation(
                string.IsNullOrWhiteSpace(FirstName) ? null : FirstName.Trim(),
                string.IsNullOrWhiteSpace(LastName) ? null : LastName.Trim(),
                Birthday,
                Age,
                EditPosition?.Id,
                EditTeam?.TeamId,
                EditNationality?.Id,
                Number,
                IsActive,
                null);
            if (IsCreating)
            {
                await _playerClient.CreatePlayerAsync(mutation, cancellationToken);
            }
            else if (SelectedPlayer is not null)
            {
                await _playerClient.UpdatePlayerAsync(SelectedPlayer.PlayerId, mutation, cancellationToken);
            }

            CancelEdit();
            await LoadPlayersOnlyAsync(cancellationToken);
            return AdminPageState.Partial("Joueuse enregistree via le contrat legacy. Audit et ETag ne sont pas disponibles.");
        });
    }

    private async Task LoadPlayersOnlyAsync(CancellationToken cancellationToken)
    {
        var result = await _playerClient.GetPlayersAsync(
            Page,
            PageSize,
            Search,
            FilterTeam?.TeamId,
            ActiveOnly,
            cancellationToken);
        Replace(Players, result.Items);
    }

    private void CancelEdit()
    {
        _selectedPlayer = null;
        OnPropertyChanged(nameof(SelectedPlayer));
        IsCreating = false;
        OnPropertyChanged(nameof(IsEditorVisible));
        OnPropertyChanged(nameof(IdentityStatus));
        SavePlayerCommand.NotifyCanExecuteChanged();
    }

    private void ScheduleSearch()
    {
        _searchSource?.Cancel();
        _searchSource?.Dispose();
        _searchSource = new CancellationTokenSource();
        _ = ApplySearchAfterDelayAsync(_searchSource.Token);
    }

    private async Task ApplySearchAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken);
            Page = 1;
            await RefreshCommand.ExecuteAsync(null);
        }
        catch (OperationCanceledException)
        {
            // A newer search value owns the next request.
        }
    }

    private async Task ApplyFiltersAsync()
    {
        Page = 1;
        await RefreshCommand.ExecuteAsync(null);
    }

    private async Task ClearFiltersAsync()
    {
        _searchSource?.Cancel();
        _search = string.Empty;
        OnPropertyChanged(nameof(Search));
        FilterTeam = null;
        ActiveOnly = true;
        Page = 1;
        await RefreshCommand.ExecuteAsync(null);
    }

    private async Task NextPageAsync()
    {
        Page++;
        await RefreshCommand.ExecuteAsync(null);
    }

    private async Task PreviousPageAsync()
    {
        if (Page > 1)
        {
            Page--;
            await RefreshCommand.ExecuteAsync(null);
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private static string ExtractFirstName(string? fullName)
    {
        var normalized = (fullName ?? string.Empty).Trim();
        var separator = normalized.IndexOf(' ');
        return separator < 0 ? normalized : normalized[..separator].Trim();
    }

    private static string ExtractLastName(string? fullName)
    {
        var normalized = (fullName ?? string.Empty).Trim();
        var separator = normalized.IndexOf(' ');
        return separator < 0 ? string.Empty : normalized[(separator + 1)..].Trim();
    }
}

public sealed class TeamsAdminViewModel(IAdminTeamApiClient apiClient) : AdminPageViewModelBase(
    "Equipes",
    "Consultez les equipes et leurs effectifs exposes par l'API.")
{
    public ObservableCollection<TeamDto> Teams { get; } = [];
    public AdminApiAvailability WriteAvailability => apiClient.WriteAvailability;

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var teams = await apiClient.GetTeamsAsync(cancellationToken);
        Teams.Clear();
        foreach (var team in teams.OrderBy(item => item.TeamName))
        {
            Teams.Add(team);
        }

        return Teams.Count == 0
            ? AdminPageState.Empty("Aucune equipe n'est disponible.")
            : AdminPageState.Partial("La liste est disponible. Creation, modification et archivage sont bloques par l'API admin.");
    }
}

public sealed class ReferenceDataViewModel(IAdminReferenceDataApiClient apiClient) : AdminPageViewModelBase(
    "Referentiels",
    "Catalogue humain en lecture seule, sans explorateur de tables ni SQL libre.")
{
    public ObservableCollection<CompetitionDto> Competitions { get; } = [];
    public ObservableCollection<LookupItemDto> Events { get; } = [];
    public ObservableCollection<LookupItemDto> Positions { get; } = [];
    public ObservableCollection<LookupItemDto> Nationalities { get; } = [];
    public ObservableCollection<LookupItemDto> Attacks { get; } = [];
    public ObservableCollection<LookupItemDto> Defenses { get; } = [];
    public AdminApiAvailability WriteAvailability => apiClient.WriteAvailability;

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var competitions = apiClient.GetCompetitionsAsync(cancellationToken);
        var events = apiClient.GetEventsAsync(cancellationToken);
        var positions = apiClient.GetPositionsAsync(cancellationToken);
        var nationalities = apiClient.GetNationalitiesAsync(cancellationToken);
        var attacks = apiClient.GetAttacksAsync(cancellationToken);
        var defenses = apiClient.GetDefensesAsync(cancellationToken);
        var competitionItems = await competitions;
        var eventItems = await events;
        var positionItems = await positions;
        var nationalityItems = await nationalities;
        var attackItems = await attacks;
        var defenseItems = await defenses;
        Replace(Competitions, competitionItems);
        Replace(Events, eventItems);
        Replace(Positions, positionItems);
        Replace(Nationalities, nationalityItems);
        Replace(Attacks, attackItems);
        Replace(Defenses, defenseItems);
        return AdminPageState.Partial("Six catalogues sont consultables. Leur administration auditee reste bloquee par l'API.");
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}

public sealed class AuditViewModel : AdminPageViewModelBase
{
    private readonly IAdminAuditApiClient _apiClient;
    private int _page = 1;
    private int _pageSize = 25;
    private string? _entityType;
    private string? _entityReference;
    private AdminAuditEntry? _selectedEntry;

    public AuditViewModel(IAdminAuditApiClient apiClient) : base(
        "Audit",
        "Recherchez les ecritures administratives et comparez les valeurs avant/apres.")
    {
        _apiClient = apiClient;
        ApplyFiltersCommand = new AsyncRelayCommand(ApplyFiltersAsync, () => !IsLoading);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage && !IsLoading);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => Page > 1 && !IsLoading);
    }

    public ObservableCollection<AdminAuditEntry> Entries { get; } = [];
    public IReadOnlyList<int> PageSizes { get; } = [25, 50, 100];
    public IAsyncRelayCommand ApplyFiltersCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public int Page { get => _page; private set { SetProperty(ref _page, value); PreviousPageCommand.NotifyCanExecuteChanged(); } }
    public int PageSize { get => _pageSize; set => SetProperty(ref _pageSize, value); }
    public long Total { get; private set; }
    public bool HasNextPage { get; private set; }
    public string? EntityType { get => _entityType; set => SetProperty(ref _entityType, value); }
    public string? EntityReference { get => _entityReference; set => SetProperty(ref _entityReference, value); }
    public AdminAuditEntry? SelectedEntry { get => _selectedEntry; set { SetProperty(ref _selectedEntry, value); OnPropertyChanged(nameof(HasSelection)); } }
    public bool HasSelection => SelectedEntry is not null;

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var result = await _apiClient.GetAuditAsync(Page, PageSize, EntityType, EntityReference, cancellationToken);
        Entries.Clear();
        foreach (var entry in result.Items)
        {
            Entries.Add(entry);
        }

        Total = result.Total ?? Entries.Count;
        HasNextPage = result.HasNextPage;
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(HasNextPage));
        NextPageCommand.NotifyCanExecuteChanged();
        return Entries.Count == 0
            ? AdminPageState.Empty("Aucune trace ne correspond aux filtres.")
            : AdminPageState.Loaded($"{Total} trace(s) auditee(s).");
    }

    private async Task ApplyFiltersAsync()
    {
        Page = 1;
        await RefreshCommand.ExecuteAsync(null);
    }

    private async Task NextPageAsync()
    {
        Page++;
        await RefreshCommand.ExecuteAsync(null);
    }

    private async Task PreviousPageAsync()
    {
        if (Page > 1)
        {
            Page--;
            await RefreshCommand.ExecuteAsync(null);
        }
    }
}

public sealed class UsersAdminViewModel : AdminPageViewModelBase
{
    private readonly IAdminUsersApiClient _apiClient;
    private readonly IAdminCapabilitiesService _capabilities;
    private AdminUser? _selectedUser;
    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _role = "Consultation";
    private bool _isActive = true;
    private bool _isCreating;

    public UsersAdminViewModel(
        IAdminUsersApiClient apiClient,
        IAdminCapabilitiesService capabilities) : base(
            "Utilisateurs et droits",
            "Administrez les comptes sans jamais exposer de mot de passe ni de hash.")
    {
        _apiClient = apiClient;
        _capabilities = capabilities;
        NewUserCommand = new RelayCommand(StartCreate, CanManage);
        SaveUserCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
    }

    public ObservableCollection<AdminUser> Users { get; } = [];
    public IReadOnlyList<string> Roles { get; } = ["Consultation", "Admin"];
    public IRelayCommand NewUserCommand { get; }
    public IAsyncRelayCommand SaveUserCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public AdminUser? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (!SetProperty(ref _selectedUser, value) || value is null)
            {
                return;
            }

            IsCreating = false;
            Username = value.Username;
            Email = value.Email ?? string.Empty;
            Password = string.Empty;
            Role = value.Role;
            IsActive = value.IsActive;
            OnPropertyChanged(nameof(IsEditorVisible));
            SaveUserCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsCreating { get => _isCreating; private set => SetProperty(ref _isCreating, value); }
    public string Username { get => _username; set { SetProperty(ref _username, value); SaveUserCommand.NotifyCanExecuteChanged(); } }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Password { get => _password; set { SetProperty(ref _password, value); SaveUserCommand.NotifyCanExecuteChanged(); } }
    public string Role { get => _role; set => SetProperty(ref _role, value); }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
    public bool IsEditorVisible => IsCreating || SelectedUser is not null;

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var users = await _apiClient.GetUsersAsync(cancellationToken);
        Users.Clear();
        foreach (var user in users)
        {
            Users.Add(user);
        }

        return Users.Count == 0
            ? AdminPageState.Empty("Aucun compte n'est disponible.")
            : AdminPageState.Partial("Les comptes sont gerables via les routes legacy; l'audit admin V2 reste incomplet.");
    }

    private bool CanManage() => _capabilities.Has(AdminPermissionNames.UsersManage);

    private bool CanSave() =>
        CanManage() && !IsLoading && !string.IsNullOrWhiteSpace(Username) &&
        (!IsCreating || Password.Length >= 8 && Password.Any(char.IsLetter) && Password.Any(char.IsDigit));

    private void StartCreate()
    {
        _selectedUser = null;
        OnPropertyChanged(nameof(SelectedUser));
        IsCreating = true;
        Username = string.Empty;
        Email = string.Empty;
        Password = string.Empty;
        Role = "Consultation";
        IsActive = true;
        OnPropertyChanged(nameof(IsEditorVisible));
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            if (IsCreating)
            {
                await _apiClient.CreateUserAsync(
                    new AdminUserCreate(Username.Trim(), Password, Email.Trim(), Role),
                    cancellationToken);
            }
            else if (SelectedUser is not null)
            {
                await _apiClient.UpdateUserAsync(
                    SelectedUser.Id,
                    new AdminUserUpdate(
                        Email.Trim(),
                        string.IsNullOrEmpty(Password) ? null : Password,
                        Role,
                        IsActive),
                    cancellationToken);
            }

            Cancel();
            var users = await _apiClient.GetUsersAsync(cancellationToken);
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }

            return AdminPageState.Partial("Compte enregistre. Le contrat legacy ne fournit pas encore un audit V2 complet.");
        });
    }

    private void Cancel()
    {
        _selectedUser = null;
        OnPropertyChanged(nameof(SelectedUser));
        IsCreating = false;
        Password = string.Empty;
        OnPropertyChanged(nameof(IsEditorVisible));
        SaveUserCommand.NotifyCanExecuteChanged();
    }
}

public sealed class SettingsViewModel(
    IAdminMaintenanceApiClient apiClient,
    HandballIntegration.Data.ApiSettings settings) : AdminPageViewModelBase(
        "Parametres et diagnostics",
        "Configuration client redigee, versions et etat de connexion.")
{
    private string _apiVersion = "Verification";
    private string _databaseVersion = "Verification";

    public string Environment => settings.EnvironmentLabel;
    public string ApiEndpoint => new Uri(settings.ApiBaseUrl).Host;
    public int TimeoutSeconds => settings.TimeoutSeconds;
    public string ApiVersion { get => _apiVersion; private set => SetProperty(ref _apiVersion, value); }
    public string DatabaseVersion { get => _databaseVersion; private set => SetProperty(ref _databaseVersion, value); }
    public AdminApiAvailability MaintenanceAvailability => apiClient.ActionAvailability;

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var version = await apiClient.GetVersionAsync(cancellationToken);
        ApiVersion = version.ApiVersion;
        DatabaseVersion = version.DatabaseVersion;
        return AdminPageState.Loaded("Les diagnostics affiches ne contiennent ni token, ni secret, ni chaine de connexion.");
    }
}
