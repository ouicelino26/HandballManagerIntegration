using System.Collections.ObjectModel;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;
using HandballIntegration.Core.Abstractions;
using HandballIntegration.Core.Models;

namespace HandballIntegration.Presentation.ViewModels;

public sealed record DashboardActionCard(
    string Title,
    string Description,
    string Status,
    string TargetTag,
    string RequiredPermission);

public sealed class DashboardViewModel(
    IAdminDashboardApiClient apiClient,
    IAdminCapabilitiesService capabilities) : AdminPageViewModelBase(
        "Centre d'administration",
        "Les controles disponibles conduisent directement vers une action autorisee.")
{
    private string _apiVersion = "Verification";
    private string _databaseVersion = "Verification";
    private string _apiStatus = "Verification";
    private DateTime? _lastCheckedAt;

    public ObservableCollection<DashboardActionCard> Actions { get; } = [];

    public string ApiVersion
    {
        get => _apiVersion;
        private set => SetProperty(ref _apiVersion, value);
    }

    public string DatabaseVersion
    {
        get => _databaseVersion;
        private set => SetProperty(ref _databaseVersion, value);
    }

    public string ApiStatus
    {
        get => _apiStatus;
        private set => SetProperty(ref _apiStatus, value);
    }

    public DateTime? LastCheckedAt
    {
        get => _lastCheckedAt;
        private set => SetProperty(ref _lastCheckedAt, value);
    }

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var version = await apiClient.GetVersionAsync(cancellationToken);
        ApiVersion = version.ApiVersion;
        DatabaseVersion = version.DatabaseVersion;
        ApiStatus = "Disponible";
        LastCheckedAt = DateTime.Now;

        Actions.Clear();
        AddIfAllowed(AdminPermissionNames.ImportsRead, "Integrations", "Previsualiser un fichier avant toute ecriture.", "CONTROLE", "integration");
        AddIfAllowed(AdminPermissionNames.MatchesRead, "Matchs", "Consulter et corriger un match avec controle de version.", "DISPONIBLE", "matches");
        AddIfAllowed(AdminPermissionNames.PlayersRead, "Joueuses", "Rechercher une identite et verifier son rattachement.", "LECTURE", "players");
        AddIfAllowed(AdminPermissionNames.AuditRead, "Audit", "Retrouver les ecritures administratives et leur correlation.", "DISPONIBLE", "audit");

        return Actions.Count == 0
            ? AdminPageState.Empty("Aucune action n'est autorisee pour cette session.")
            : AdminPageState.Loaded($"{Actions.Count} espace(s) actionnable(s) disponible(s).");
    }

    private void AddIfAllowed(string permission, string title, string description, string status, string tag)
    {
        if (capabilities.Has(permission))
        {
            Actions.Add(new DashboardActionCard(title, description, status, tag, permission));
        }
    }
}
