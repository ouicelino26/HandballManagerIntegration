using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;

namespace HandballIntegration.Admin.Services;

public sealed class AdminNavigationService : IAdminNavigationService
{
    private static readonly IReadOnlyList<AdminNavigationItem> Modules =
    [
        new("dashboard", "Accueil", "Etat de la plateforme", "01", AdminPermissionNames.DashboardRead, AdminModuleStatus.FoundationReady, true),
        new("integration", "Integrations", "Apercu puis execution", "02", AdminPermissionNames.ImportsRead, AdminModuleStatus.Partial, true),
        new("matches", "Matchs", "Consultation et cycle de vie", "03", AdminPermissionNames.MatchesRead, AdminModuleStatus.ReadOnlyAvailable, false),
        new("players", "Joueuses", "Fiches et disponibilite", "04", AdminPermissionNames.PlayersRead, AdminModuleStatus.Partial, true),
        new("teams", "Equipes", "Effectifs et identite", "05", AdminPermissionNames.TeamsRead, AdminModuleStatus.NotImplemented, false),
        new("events", "Evenements", "Actions de match", "06", AdminPermissionNames.EventsRead, AdminModuleStatus.ReadOnlyAvailable, false),
        new("reference-data", "Referentiels", "Valeurs partagees", "07", AdminPermissionNames.ReferenceDataManage, AdminModuleStatus.NotImplemented, false),
        new("data-quality", "Qualite des donnees", "Conflits et corrections", "08", AdminPermissionNames.DataQualityManage, AdminModuleStatus.NotImplemented, false),
        new("audit", "Historique et audit", "Traces administratives", "09", AdminPermissionNames.AuditRead, AdminModuleStatus.ReadOnlyAvailable, false),
        new("maintenance", "Maintenance", "Diagnostics controles", "10", AdminPermissionNames.DashboardRead, AdminModuleStatus.NotImplemented, false),
        new("users", "Utilisateurs et droits", "Comptes et acces", "11", AdminPermissionNames.UsersManage, AdminModuleStatus.Partial, true),
        new("settings", "Parametres", "Environnement et versions", "12", AdminPermissionNames.DashboardRead, AdminModuleStatus.FoundationReady, false)
    ];

    public IReadOnlyList<AdminNavigationItem> Build(IReadOnlySet<string> permissions) =>
        Modules.Where(item => permissions.Contains(item.RequiredPermission)).ToArray();

    public bool CanAccess(string tag, IReadOnlySet<string> permissions) =>
        Modules.Any(item =>
            string.Equals(item.Tag, tag, StringComparison.Ordinal) &&
            permissions.Contains(item.RequiredPermission));
}
