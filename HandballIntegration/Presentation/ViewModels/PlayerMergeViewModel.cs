using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HandballIntegration.Admin.Models;
using HandballIntegration.Core.Abstractions;
using HandballIntegration.Core.Models;
using HandballManagerCore.DTO;

namespace HandballIntegration.Presentation.ViewModels;

public sealed record DuplicatePlayerGroup(
    string NormalizedKey,
    string DisplayName,
    IReadOnlyList<PlayerListItemDto> Players);

public sealed class PlayerMergeViewModel : AdminPageViewModelBase
{
    private readonly IAdminPlayerApiClient _playerClient;
    private readonly IAdminCapabilitiesService _capabilities;
    private DuplicatePlayerGroup? _selectedDuplicateGroup;
    private PlayerListItemDto? _selectedSource;
    private PlayerListItemDto? _selectedTarget;
    private string _sourceSearchQuery = string.Empty;
    private string _targetSearchQuery = string.Empty;
    private PlayerMergeResult? _lastMergeResult;

    public PlayerMergeViewModel(
        IAdminPlayerApiClient playerClient,
        IAdminCapabilitiesService capabilities) : base(
            "Fusion de joueuses",
            "Detectez les doublons et fusionnez les identites en preservant tous les evenements.")
    {
        _playerClient = playerClient;
        _capabilities = capabilities;
        LoadDuplicatesCommand = new AsyncRelayCommand(LoadDuplicatesAsync, () => !IsLoading);
        SearchSourceCommand = new AsyncRelayCommand(SearchSourceAsync, () => !IsLoading);
        SearchTargetCommand = new AsyncRelayCommand(SearchTargetAsync, () => !IsLoading);
        MergeCommand = new AsyncRelayCommand(MergeAsync, () => CanMerge);
    }

    public ObservableCollection<DuplicatePlayerGroup> DuplicateGroups { get; } = [];
    public ObservableCollection<PlayerListItemDto> SourceSearchResults { get; } = [];
    public ObservableCollection<PlayerListItemDto> TargetSearchResults { get; } = [];

    public IAsyncRelayCommand LoadDuplicatesCommand { get; }
    public IAsyncRelayCommand SearchSourceCommand { get; }
    public IAsyncRelayCommand SearchTargetCommand { get; }
    public IAsyncRelayCommand MergeCommand { get; }

    public DuplicatePlayerGroup? SelectedDuplicateGroup
    {
        get => _selectedDuplicateGroup;
        set
        {
            if (!SetProperty(ref _selectedDuplicateGroup, value) || value is null)
            {
                return;
            }

            SelectedSource = value.Players.Count > 0 ? value.Players[0] : null;
            SelectedTarget = value.Players.Count > 1 ? value.Players[1] : null;
        }
    }

    public PlayerListItemDto? SelectedSource
    {
        get => _selectedSource;
        set
        {
            SetProperty(ref _selectedSource, value);
            OnPropertyChanged(nameof(CanMerge));
            OnPropertyChanged(nameof(MergeSummary));
            MergeCommand.NotifyCanExecuteChanged();
        }
    }

    public PlayerListItemDto? SelectedTarget
    {
        get => _selectedTarget;
        set
        {
            SetProperty(ref _selectedTarget, value);
            OnPropertyChanged(nameof(CanMerge));
            OnPropertyChanged(nameof(MergeSummary));
            MergeCommand.NotifyCanExecuteChanged();
        }
    }

    public string SourceSearchQuery
    {
        get => _sourceSearchQuery;
        set => SetProperty(ref _sourceSearchQuery, value);
    }

    public string TargetSearchQuery
    {
        get => _targetSearchQuery;
        set => SetProperty(ref _targetSearchQuery, value);
    }

    public bool CanMerge =>
        SelectedSource is not null &&
        SelectedTarget is not null &&
        SelectedSource.PlayerId != SelectedTarget.PlayerId;

    public string MergeSummary =>
        CanMerge
            ? $"Fusionner \"{SelectedSource!.FullName}\" → \"{SelectedTarget!.FullName}\""
            : "Selectionnez une source et une cible distinctes.";

    public PlayerMergeResult? LastMergeResult
    {
        get => _lastMergeResult;
        private set
        {
            SetProperty(ref _lastMergeResult, value);
            OnPropertyChanged(nameof(HasMergeResult));
        }
    }

    public bool HasMergeResult => LastMergeResult is not null;

    protected override Task<AdminPageState> LoadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(AdminPageState.Idle(
            "Detectez les doublons ou effectuez une selection manuelle."));

    private async Task LoadDuplicatesAsync()
    {
        if (!_capabilities.Has(AdminPermissionNames.PlayersMerge))
        {
            State = AdminPageState.FromError(
                System.Net.HttpStatusCode.Forbidden,
                "ADMIN_FORBIDDEN",
                "La permission Players.Merge est requise pour detecter les doublons.",
                null);
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            var all = await FetchAllPlayersAsync(cancellationToken);
            var groups = BuildDuplicateGroups(all);

            DuplicateGroups.Clear();
            foreach (var group in groups)
            {
                DuplicateGroups.Add(group);
            }

            LoadDuplicatesCommand.NotifyCanExecuteChanged();

            return groups.Count == 0
                ? AdminPageState.Empty("Aucun doublon detecte parmi les joueuses actives et inactives.")
                : AdminPageState.Loaded($"{groups.Count} groupe(s) de doublons detecte(s).");
        });
    }

    private async Task SearchSourceAsync()
    {
        await RunCommandAsync(async cancellationToken =>
        {
            var result = await _playerClient.GetPlayersAsync(
                1, 10, SourceSearchQuery, null, null, cancellationToken);
            SourceSearchResults.Clear();
            foreach (var player in result.Items)
            {
                SourceSearchResults.Add(player);
            }

            SearchSourceCommand.NotifyCanExecuteChanged();
            return AdminPageState.Idle(
                $"{result.Items.Count} joueuse(s) trouvee(s) pour la source.");
        });
    }

    private async Task SearchTargetAsync()
    {
        await RunCommandAsync(async cancellationToken =>
        {
            var result = await _playerClient.GetPlayersAsync(
                1, 10, TargetSearchQuery, null, null, cancellationToken);
            TargetSearchResults.Clear();
            foreach (var player in result.Items)
            {
                TargetSearchResults.Add(player);
            }

            SearchTargetCommand.NotifyCanExecuteChanged();
            return AdminPageState.Idle(
                $"{result.Items.Count} joueuse(s) trouvee(s) pour la cible.");
        });
    }

    private async Task MergeAsync()
    {
        if (!CanMerge || SelectedSource is null || SelectedTarget is null)
        {
            return;
        }

        var sourceId = SelectedSource.PlayerId;
        var targetId = SelectedTarget.PlayerId;
        var hadDuplicates = DuplicateGroups.Count > 0;

        await RunCommandAsync(async cancellationToken =>
        {
            LastMergeResult = null;

            var result = await _playerClient.MergePlayerAsync(targetId, sourceId, cancellationToken);
            LastMergeResult = result;

            SelectedSource = null;
            SelectedTarget = null;

            // Refresh the duplicate list if it was previously loaded.
            if (hadDuplicates)
            {
                var all = await FetchAllPlayersAsync(cancellationToken);
                var groups = BuildDuplicateGroups(all);
                DuplicateGroups.Clear();
                foreach (var group in groups)
                {
                    DuplicateGroups.Add(group);
                }
            }

            return AdminPageState.Loaded(
                $"Fusion terminee : {result.MergedEventsCount} evenement(s) et " +
                $"{result.MergedTimePlayersCount} temps de jeu reassignes vers \"{result.TargetPlayerName}\".");
        });
    }

    private async Task<List<PlayerListItemDto>> FetchAllPlayersAsync(CancellationToken cancellationToken)
    {
        const int batchSize = 200;
        var all = new List<PlayerListItemDto>();
        var page = 1;
        while (true)
        {
            var result = await _playerClient.GetPlayersAsync(
                page, batchSize, null, null, null, cancellationToken);
            all.AddRange(result.Items);
            if (result.Items.Count < batchSize || all.Count >= (result.Total ?? 0))
            {
                break;
            }

            page++;
        }

        return all;
    }

    private static List<DuplicatePlayerGroup> BuildDuplicateGroups(List<PlayerListItemDto> players) =>
        players
            .GroupBy(p => NormalizeName(p.FullName))
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicatePlayerGroup(
                g.Key,
                g.First().FullName,
                g.ToList()))
            .OrderBy(g => g.DisplayName)
            .ToList();

    private static string NormalizeName(string? fullName) =>
        (fullName ?? string.Empty).ToUpperInvariant().Replace(" ", string.Empty);
}
