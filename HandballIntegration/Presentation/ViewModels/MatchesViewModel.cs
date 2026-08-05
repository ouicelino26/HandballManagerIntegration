using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;
using HandballIntegration.Core.Abstractions;
using HandballIntegration.Core.Models;
using HandballManagerCore.DTO;

namespace HandballIntegration.Presentation.ViewModels;

public sealed class MatchesViewModel : AdminPageViewModelBase
{
    private readonly IAdminMatchApiClient _matchClient;
    private readonly IAdminEventApiClient _eventClient;
    private readonly IAdminTeamApiClient _teamClient;
    private readonly IAdminReferenceDataApiClient _referenceClient;
    private readonly IAdminCapabilitiesService _capabilities;
    private AdminMatchListItemDto? _selectedMatch;
    private AdminMatch? _detail;
    private AdminEventListItemDto? _selectedEvent;
    private AdminMatchEvent? _eventDetail;
    private AdminDeletionImpact? _impact;
    private AdminDeletionImpact? _eventImpact;
    private int _page = 1;
    private int _pageSize = 25;
    private string? _season;
    private string? _day;
    private AdminTeamListItemDto? _filterTeam;
    private DateTime? _from;
    private DateTime? _to;
    private CompetitionDto? _editCompetition;
    private AdminTeamListItemDto? _editHomeTeam;
    private AdminTeamListItemDto? _editAwayTeam;
    private DateTime? _editDate;
    private string? _editSeason;
    private string? _editDay;
    private int? _editHomeScore;
    private int? _editAwayScore;
    private string _editReason = string.Empty;
    private string _lifecycleReason = string.Empty;
    private string _confirmationPhrase = string.Empty;
    private string _eventReason = string.Empty;
    private string _eventConfirmationPhrase = string.Empty;
    private LookupItemDto? _editEventType;
    private AdminTeamListItemDto? _editEventTeam;
    private string _editEventTime = string.Empty;
    private string? _editEventPeriod;
    private int? _editEventHomeScore;
    private int? _editEventAwayScore;
    private string? _editEventAction;
    private bool? _editEventGoal;

    public MatchesViewModel(
        IAdminMatchApiClient matchClient,
        IAdminEventApiClient eventClient,
        IAdminTeamApiClient teamClient,
        IAdminReferenceDataApiClient referenceClient,
        IAdminCapabilitiesService capabilities) : base(
            "Matchs et evenements",
            "Consultez les matchs, corrigez-les avec ETag et inspectez chaque impact avant archivage.")
    {
        _matchClient = matchClient;
        _eventClient = eventClient;
        _teamClient = teamClient;
        _referenceClient = referenceClient;
        _capabilities = capabilities;
        OpenMatchCommand = new AsyncRelayCommand(OpenMatchAsync, () => SelectedMatch is not null && !IsLoading);
        SaveMatchCommand = new AsyncRelayCommand(SaveMatchAsync, CanSaveMatch);
        AnalyzeMatchImpactCommand = new AsyncRelayCommand(AnalyzeMatchImpactAsync, CanArchiveMatch);
        ArchiveMatchCommand = new AsyncRelayCommand(ArchiveMatchAsync, CanSubmitMatchArchive);
        RestoreMatchCommand = new AsyncRelayCommand(RestoreMatchAsync, CanSubmitMatchRestore);
        OpenEventCommand = new AsyncRelayCommand(OpenEventAsync, () => SelectedEvent is not null && !IsLoading);
        SaveEventCommand = new AsyncRelayCommand(SaveEventAsync, CanSaveEvent);
        AnalyzeEventImpactCommand = new AsyncRelayCommand(AnalyzeEventImpactAsync, CanArchiveEvent);
        ArchiveEventCommand = new AsyncRelayCommand(ArchiveEventAsync, CanSubmitEventArchive);
        RestoreEventCommand = new AsyncRelayCommand(RestoreEventAsync, CanSubmitEventRestore);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => Matches.Count == PageSize && !IsLoading);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => Page > 1 && !IsLoading);
        ApplyFiltersCommand = new AsyncRelayCommand(ApplyFiltersAsync, () => !IsLoading);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => !IsLoading);
    }

    public ObservableCollection<AdminMatchListItemDto> Matches { get; } = [];
    public ObservableCollection<AdminEventListItemDto> Events { get; } = [];
    public ObservableCollection<AdminTeamListItemDto> Teams { get; } = [];
    public ObservableCollection<CompetitionDto> Competitions { get; } = [];
    public ObservableCollection<LookupItemDto> EventTypes { get; } = [];
    public IReadOnlyList<int> PageSizes { get; } = [25, 50, 100];
    public IAsyncRelayCommand OpenMatchCommand { get; }
    public IAsyncRelayCommand SaveMatchCommand { get; }
    public IAsyncRelayCommand AnalyzeMatchImpactCommand { get; }
    public IAsyncRelayCommand ArchiveMatchCommand { get; }
    public IAsyncRelayCommand RestoreMatchCommand { get; }
    public IAsyncRelayCommand OpenEventCommand { get; }
    public IAsyncRelayCommand SaveEventCommand { get; }
    public IAsyncRelayCommand AnalyzeEventImpactCommand { get; }
    public IAsyncRelayCommand ArchiveEventCommand { get; }
    public IAsyncRelayCommand RestoreEventCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public IAsyncRelayCommand ApplyFiltersCommand { get; }
    public IAsyncRelayCommand ClearFiltersCommand { get; }

    public AdminMatchListItemDto? SelectedMatch
    {
        get => _selectedMatch;
        set
        {
            SetProperty(ref _selectedMatch, value);
            OpenMatchCommand.NotifyCanExecuteChanged();
        }
    }

    public AdminMatch? Detail
    {
        get => _detail;
        private set
        {
            SetProperty(ref _detail, value);
            OnPropertyChanged(nameof(HasDetail));
            NotifyMatchActions();
        }
    }

    public bool HasDetail => Detail is not null;

    public AdminEventListItemDto? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            SetProperty(ref _selectedEvent, value);
            OpenEventCommand.NotifyCanExecuteChanged();
        }
    }

    public AdminMatchEvent? EventDetail
    {
        get => _eventDetail;
        private set
        {
            SetProperty(ref _eventDetail, value);
            OnPropertyChanged(nameof(HasEventDetail));
            NotifyEventActions();
        }
    }

    public bool HasEventDetail => EventDetail is not null;

    public AdminDeletionImpact? Impact
    {
        get => _impact;
        private set
        {
            SetProperty(ref _impact, value);
            NotifyMatchActions();
        }
    }

    public AdminDeletionImpact? EventImpact
    {
        get => _eventImpact;
        private set
        {
            SetProperty(ref _eventImpact, value);
            NotifyEventActions();
        }
    }

    public int Page
    {
        get => _page;
        private set
        {
            SetProperty(ref _page, value);
            PreviousPageCommand.NotifyCanExecuteChanged();
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    public string? Season
    {
        get => _season;
        set => SetProperty(ref _season, value);
    }

    public string? Day
    {
        get => _day;
        set => SetProperty(ref _day, value);
    }

    public AdminTeamListItemDto? FilterTeam
    {
        get => _filterTeam;
        set => SetProperty(ref _filterTeam, value);
    }

    public DateTime? From
    {
        get => _from;
        set => SetProperty(ref _from, value);
    }

    public DateTime? To
    {
        get => _to;
        set => SetProperty(ref _to, value);
    }

    public CompetitionDto? EditCompetition
    {
        get => _editCompetition;
        set
        {
            SetProperty(ref _editCompetition, value);
            SaveMatchCommand.NotifyCanExecuteChanged();
        }
    }

    public AdminTeamListItemDto? EditHomeTeam
    {
        get => _editHomeTeam;
        set
        {
            SetProperty(ref _editHomeTeam, value);
            SaveMatchCommand.NotifyCanExecuteChanged();
        }
    }

    public AdminTeamListItemDto? EditAwayTeam
    {
        get => _editAwayTeam;
        set
        {
            SetProperty(ref _editAwayTeam, value);
            SaveMatchCommand.NotifyCanExecuteChanged();
        }
    }

    public DateTime? EditDate { get => _editDate; set => SetProperty(ref _editDate, value); }
    public string? EditSeason { get => _editSeason; set => SetProperty(ref _editSeason, value); }
    public string? EditDay { get => _editDay; set => SetProperty(ref _editDay, value); }
    public int? EditHomeScore { get => _editHomeScore; set => SetProperty(ref _editHomeScore, value); }
    public int? EditAwayScore { get => _editAwayScore; set => SetProperty(ref _editAwayScore, value); }

    public string EditReason
    {
        get => _editReason;
        set
        {
            SetProperty(ref _editReason, value);
            SaveMatchCommand.NotifyCanExecuteChanged();
        }
    }

    public string LifecycleReason
    {
        get => _lifecycleReason;
        set
        {
            SetProperty(ref _lifecycleReason, value);
            NotifyMatchActions();
        }
    }

    public string ConfirmationPhrase
    {
        get => _confirmationPhrase;
        set
        {
            SetProperty(ref _confirmationPhrase, value);
            NotifyMatchActions();
        }
    }

    public string EventReason
    {
        get => _eventReason;
        set
        {
            SetProperty(ref _eventReason, value);
            NotifyEventActions();
        }
    }

    public string EventConfirmationPhrase
    {
        get => _eventConfirmationPhrase;
        set
        {
            SetProperty(ref _eventConfirmationPhrase, value);
            NotifyEventActions();
        }
    }

    public LookupItemDto? EditEventType { get => _editEventType; set { SetProperty(ref _editEventType, value); SaveEventCommand.NotifyCanExecuteChanged(); } }
    public AdminTeamListItemDto? EditEventTeam { get => _editEventTeam; set => SetProperty(ref _editEventTeam, value); }
    public string EditEventTime { get => _editEventTime; set { SetProperty(ref _editEventTime, value); SaveEventCommand.NotifyCanExecuteChanged(); } }
    public string? EditEventPeriod { get => _editEventPeriod; set => SetProperty(ref _editEventPeriod, value); }
    public int? EditEventHomeScore { get => _editEventHomeScore; set { SetProperty(ref _editEventHomeScore, value); SaveEventCommand.NotifyCanExecuteChanged(); } }
    public int? EditEventAwayScore { get => _editEventAwayScore; set { SetProperty(ref _editEventAwayScore, value); SaveEventCommand.NotifyCanExecuteChanged(); } }
    public string? EditEventAction { get => _editEventAction; set => SetProperty(ref _editEventAction, value); }
    public bool? EditEventGoal { get => _editEventGoal; set => SetProperty(ref _editEventGoal, value); }

    public bool CanCreateEvent => false;
    public string EventCreateStatus => "BLOCKED_BY_API - aucune route POST V2 auditee n'est deployee.";

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var matchesTask = _matchClient.GetMatchesAsync(
            Page,
            PageSize,
            Season,
            Day,
            FilterTeam?.TeamId,
            From,
            To,
            cancellationToken: cancellationToken);
        var teamsTask = _teamClient.GetTeamsAsync(cancellationToken: cancellationToken);
        var competitionsTask = _referenceClient.GetCompetitionsAsync(cancellationToken);
        var eventTypesTask = _referenceClient.GetEventsAsync(cancellationToken);
        var matches = await matchesTask;
        var teamsPage = await teamsTask;
        var competitions = await competitionsTask;
        var eventTypes = await eventTypesTask;

        Matches.Clear();
        foreach (var match in matches.Items)
        {
            Matches.Add(match);
        }

        Replace(Teams, teamsPage.Items);
        Replace(Competitions, competitions);
        Replace(EventTypes, eventTypes);
        NextPageCommand.NotifyCanExecuteChanged();
        return Matches.Count == 0
            ? AdminPageState.Empty("Aucun match ne correspond aux filtres selectionnes.")
            : AdminPageState.Partial("Liste paginee disponible. Le tri et le total serveur necessitent encore une route V2 de liste.");
    }

    private async Task OpenMatchAsync()
    {
        if (SelectedMatch is null)
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            var result = await _matchClient.GetMatchAsync(SelectedMatch.MatchId, cancellationToken);
            Detail = result.Value with { ETag = result.ETag ?? result.Value.ETag };
            EditCompetition = Competitions.FirstOrDefault(item => item.CompetitionId == Detail.CompetitionId);
            EditHomeTeam = Teams.FirstOrDefault(item => item.TeamId == Detail.Team1Id);
            EditAwayTeam = Teams.FirstOrDefault(item => item.TeamId == Detail.Team2Id);
            EditDate = Detail.Date;
            EditSeason = Detail.Season;
            EditDay = Detail.Day;
            EditHomeScore = Detail.Team1Score;
            EditAwayScore = Detail.Team2Score;
            EditReason = string.Empty;
            Impact = null;
            LifecycleReason = string.Empty;
            ConfirmationPhrase = string.Empty;

            Events.Clear();
            if (_capabilities.Has(AdminPermissionNames.EventsRead))
            {
                var eventsPage = await _eventClient.GetEventsAsync(Detail.MatchId, cancellationToken: cancellationToken);
                Replace(Events, eventsPage.Items);
            }

            EventDetail = null;
            return AdminPageState.Loaded("Le detail et sa version de concurrence sont charges.");
        });
    }

    private bool CanSaveMatch() =>
        Detail is not null &&
        _capabilities.Has(AdminPermissionNames.MatchesUpdate) &&
        !string.IsNullOrWhiteSpace(EditReason) &&
        EditHomeTeam is not null &&
        EditAwayTeam is not null &&
        EditHomeTeam.TeamId != EditAwayTeam.TeamId &&
        (EditHomeScore ?? 0) >= 0 &&
        (EditAwayScore ?? 0) >= 0 &&
        !IsLoading;

    private async Task SaveMatchAsync()
    {
        if (!CanSaveMatch() || Detail is null || EditHomeTeam is null || EditAwayTeam is null)
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            var request = new AdminMatchUpdate(
                EditCompetition?.CompetitionId,
                EditDate,
                EditHomeTeam.TeamId,
                EditAwayTeam.TeamId,
                EditHomeScore,
                EditAwayScore,
                EditDate?.Year,
                EditSeason?.Trim(),
                EditDay?.Trim(),
                EditReason.Trim());
            var result = await _matchClient.UpdateMatchAsync(
                Detail.MatchId,
                request,
                Detail.ETag,
                cancellationToken);
            Detail = result.Value with { ETag = result.ETag ?? result.Value.ETag };
            EditReason = string.Empty;
            return AdminPageState.Loaded("Match enregistre avec controle de concurrence et trace d'audit.");
        });
    }

    private bool CanArchiveMatch() =>
        Detail is not null && _capabilities.Has(AdminPermissionNames.MatchesDelete) && !IsLoading;

    private async Task AnalyzeMatchImpactAsync()
    {
        if (!CanArchiveMatch() || Detail is null)
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            Impact = await _matchClient.GetDeletionImpactAsync(Detail.MatchId, cancellationToken);
            return Impact.BlockingReasons.Count > 0
                ? AdminPageState.Partial("L'analyse d'impact signale des blocages.")
                : AdminPageState.Loaded("Impact charge. Verifiez chaque dependance avant de confirmer.");
        });
    }

    private bool CanSubmitMatchArchive() =>
        CanArchiveMatch() &&
        Detail?.State == "ACTIVE" &&
        Impact is { CanDelete: true } &&
        !string.IsNullOrWhiteSpace(LifecycleReason) &&
        string.Equals(ConfirmationPhrase.Trim(), "ARCHIVER", StringComparison.Ordinal);

    private bool CanSubmitMatchRestore() =>
        CanArchiveMatch() &&
        Detail?.State == "ARCHIVED" &&
        Impact is not null &&
        !string.IsNullOrWhiteSpace(LifecycleReason) &&
        string.Equals(ConfirmationPhrase.Trim(), "RESTAURER", StringComparison.Ordinal);

    private Task ArchiveMatchAsync() => SubmitMatchLifecycleAsync(restore: false);
    private Task RestoreMatchAsync() => SubmitMatchLifecycleAsync(restore: true);

    private async Task SubmitMatchLifecycleAsync(bool restore)
    {
        if (Detail is null || Impact is null || restore && !CanSubmitMatchRestore() || !restore && !CanSubmitMatchArchive())
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            var request = new AdminLifecycleRequest(
                LifecycleReason.Trim(),
                Impact.CurrentVersion,
                Impact.CurrentEtag,
                null,
                Impact.ConfirmationToken);
            if (restore)
            {
                await _matchClient.RestoreAsync(Detail.MatchId, request, Detail.ETag, cancellationToken);
            }
            else
            {
                await _matchClient.ArchiveAsync(Detail.MatchId, request, Detail.ETag, cancellationToken);
            }

            var refreshed = await _matchClient.GetMatchAsync(Detail.MatchId, cancellationToken);
            Detail = refreshed.Value with { ETag = refreshed.ETag ?? refreshed.Value.ETag };
            Impact = null;
            LifecycleReason = string.Empty;
            ConfirmationPhrase = string.Empty;
            return AdminPageState.Loaded(restore ? "Match restaure et audite." : "Match archive et audite.");
        });
    }

    private async Task OpenEventAsync()
    {
        if (Detail is null || SelectedEvent is null)
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            var result = await _eventClient.GetEventAsync(Detail.MatchId, SelectedEvent.Id, cancellationToken);
            EventDetail = result.Value with { ETag = result.ETag ?? result.Value.ETag };
            EventReason = string.Empty;
            EventImpact = null;
            EventConfirmationPhrase = string.Empty;
            EditEventType = EventTypes.FirstOrDefault(item => item.Id == EventDetail.EventId);
            EditEventTeam = Teams.FirstOrDefault(item => item.TeamId == EventDetail.TeamId);
            EditEventTime = EventDetail.Time?.ToString(@"hh\:mm\:ss") ?? string.Empty;
            EditEventPeriod = EventDetail.Period;
            EditEventHomeScore = EventDetail.TeamScore1;
            EditEventAwayScore = EventDetail.TeamScore2;
            EditEventAction = EventDetail.Action;
            EditEventGoal = EventDetail.Goal;
            return AdminPageState.Loaded("Evenement charge avec sa version de concurrence.");
        });
    }

    private bool CanSaveEvent() =>
        EventDetail is not null &&
        _capabilities.Has(AdminPermissionNames.EventsUpdate) &&
        EditEventType is not null &&
        (string.IsNullOrWhiteSpace(EditEventTime) || TimeSpan.TryParse(EditEventTime, out _)) &&
        (EditEventHomeScore ?? 0) >= 0 &&
        (EditEventAwayScore ?? 0) >= 0 &&
        !string.IsNullOrWhiteSpace(EventReason) &&
        !IsLoading;

    private async Task SaveEventAsync()
    {
        if (!CanSaveEvent() || Detail is null || EventDetail is null || EditEventType is null)
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            TimeSpan? eventTime = TimeSpan.TryParse(EditEventTime, out var parsedTime) ? parsedTime : null;
            var request = new AdminMatchEventUpdate(
                EventDetail.PlayerId,
                eventTime,
                EditEventPeriod,
                EditEventHomeScore,
                EditEventAwayScore,
                EditEventType.Id,
                EditEventTeam?.TeamId,
                EventDetail.AttackId,
                EventDetail.DefenseId,
                EditEventAction,
                null,
                null,
                null,
                null,
                null,
                EditEventGoal,
                null,
                EventReason.Trim());
            var result = await _eventClient.UpdateEventAsync(
                Detail.MatchId,
                EventDetail.MatchEventId,
                request,
                EventDetail.ETag,
                cancellationToken);
            EventDetail = result.Value with { ETag = result.ETag ?? result.Value.ETag };
            EventReason = string.Empty;
            return AdminPageState.Loaded("Evenement enregistre et audite.");
        });
    }

    private bool CanArchiveEvent() =>
        Detail is not null && EventDetail is not null &&
        _capabilities.Has(AdminPermissionNames.EventsDelete) && !IsLoading;

    private async Task AnalyzeEventImpactAsync()
    {
        if (!CanArchiveEvent() || Detail is null || EventDetail is null)
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            EventImpact = await _eventClient.GetDeletionImpactAsync(
                Detail.MatchId,
                EventDetail.MatchEventId,
                cancellationToken);
            return AdminPageState.Loaded("Impact sur le score et les statistiques charge.");
        });
    }

    private bool CanSubmitEventArchive() =>
        CanArchiveEvent() && EventDetail?.State == "ACTIVE" && EventImpact is { CanDelete: true } &&
        !string.IsNullOrWhiteSpace(EventReason) &&
        string.Equals(EventConfirmationPhrase.Trim(), "ARCHIVER", StringComparison.Ordinal);

    private bool CanSubmitEventRestore() =>
        CanArchiveEvent() && EventDetail?.State == "ARCHIVED" && EventImpact is not null &&
        !string.IsNullOrWhiteSpace(EventReason) &&
        string.Equals(EventConfirmationPhrase.Trim(), "RESTAURER", StringComparison.Ordinal);

    private Task ArchiveEventAsync() => SubmitEventLifecycleAsync(restore: false);
    private Task RestoreEventAsync() => SubmitEventLifecycleAsync(restore: true);

    private async Task SubmitEventLifecycleAsync(bool restore)
    {
        if (Detail is null || EventDetail is null || EventImpact is null ||
            restore && !CanSubmitEventRestore() || !restore && !CanSubmitEventArchive())
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            var request = new AdminLifecycleRequest(
                EventReason.Trim(),
                EventImpact.CurrentVersion,
                EventImpact.CurrentEtag,
                null,
                EventImpact.ConfirmationToken);
            if (restore)
            {
                await _eventClient.RestoreAsync(
                    Detail.MatchId,
                    EventDetail.MatchEventId,
                    request,
                    EventDetail.ETag,
                    cancellationToken);
            }
            else
            {
                await _eventClient.ArchiveAsync(
                    Detail.MatchId,
                    EventDetail.MatchEventId,
                    request,
                    EventDetail.ETag,
                    cancellationToken);
            }

            var refreshed = await _eventClient.GetEventAsync(
                Detail.MatchId,
                EventDetail.MatchEventId,
                cancellationToken);
            EventDetail = refreshed.Value with { ETag = refreshed.ETag ?? refreshed.Value.ETag };
            EventImpact = null;
            EventReason = string.Empty;
            EventConfirmationPhrase = string.Empty;
            return AdminPageState.Loaded(restore ? "Evenement restaure et recalcule." : "Evenement archive et recalcule.");
        });
    }

    private async Task NextPageAsync()
    {
        Page++;
        await RefreshCommand.ExecuteAsync(null);
    }

    private async Task PreviousPageAsync()
    {
        if (Page <= 1)
        {
            return;
        }

        Page--;
        await RefreshCommand.ExecuteAsync(null);
    }

    private async Task ApplyFiltersAsync()
    {
        Page = 1;
        await RefreshCommand.ExecuteAsync(null);
    }

    private async Task ClearFiltersAsync()
    {
        Season = null;
        Day = null;
        FilterTeam = null;
        From = null;
        To = null;
        Page = 1;
        await RefreshCommand.ExecuteAsync(null);
    }

    private void NotifyMatchActions()
    {
        SaveMatchCommand.NotifyCanExecuteChanged();
        AnalyzeMatchImpactCommand.NotifyCanExecuteChanged();
        ArchiveMatchCommand.NotifyCanExecuteChanged();
        RestoreMatchCommand.NotifyCanExecuteChanged();
    }

    private void NotifyEventActions()
    {
        SaveEventCommand.NotifyCanExecuteChanged();
        AnalyzeEventImpactCommand.NotifyCanExecuteChanged();
        ArchiveEventCommand.NotifyCanExecuteChanged();
        RestoreEventCommand.NotifyCanExecuteChanged();
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
