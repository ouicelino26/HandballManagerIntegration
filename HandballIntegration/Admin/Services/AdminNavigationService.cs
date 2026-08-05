using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;

namespace HandballIntegration.Admin.Services;

public sealed class AdminNavigationService : IAdminNavigationService
{
    private static readonly IReadOnlyList<AdminNavigationItem> Modules =
    [
        new("dashboard", "Accueil", "Etat et actions disponibles", "AC", AdminPermissionNames.DashboardRead, AdminModuleStatus.Partial, true),
        new("integration", "Integrations", "Preview, decision et rapport", "IN", AdminPermissionNames.ImportsRead, AdminModuleStatus.Partial, true),
        new("matches", "Matchs", "Correction et cycle de vie", "MA", AdminPermissionNames.MatchesRead, AdminModuleStatus.Partial, true),
        new("events", "Evenements", "Depuis le contexte d'un match", "EV", AdminPermissionNames.EventsRead, AdminModuleStatus.Partial, true),
        new("players", "Joueuses", "Identites et rattachements", "JO", AdminPermissionNames.PlayersRead, AdminModuleStatus.Partial, true),
        new("teams", "Equipes", "Effectifs et identite", "EQ", AdminPermissionNames.TeamsRead, AdminModuleStatus.Partial, true),
        new("reference-data", "Referentiels", "Catalogues autorises", "RE", AdminPermissionNames.ReferenceDataManage, AdminModuleStatus.Partial, true),
        new("data-quality", "Qualite des donnees", "Anomalies et resolutions", "QU", AdminPermissionNames.DataQualityManage, AdminModuleStatus.BlockedByApi, false),
        new("reconciliation", "Reconciliation", "Identites et conflits", "RC", AdminPermissionNames.DataQualityManage, AdminModuleStatus.BlockedByApi, false),
        new("import-history", "Historique des imports", "Executions et rapports", "HI", AdminPermissionNames.ImportsRead, AdminModuleStatus.BlockedByApi, false),
        new("audit", "Audit", "Traces et differences", "AU", AdminPermissionNames.AuditRead, AdminModuleStatus.Available, true),
        new("maintenance", "Maintenance", "Diagnostics controles", "MN", AdminPermissionNames.DashboardRead, AdminModuleStatus.Partial, true),
        new("users", "Utilisateurs et droits", "Comptes et acces", "UT", AdminPermissionNames.UsersManage, AdminModuleStatus.Partial, true),
        new("settings", "Parametres", "Environnement et versions", "PA", AdminPermissionNames.DashboardRead, AdminModuleStatus.FoundationReady, true)
    ];

    public IReadOnlyList<AdminNavigationItem> Build(IReadOnlySet<string> permissions) =>
        Modules.Where(item => permissions.Contains(item.RequiredPermission)).ToArray();

    public bool CanAccess(string tag, IReadOnlySet<string> permissions) =>
        Modules.Any(item =>
            string.Equals(item.Tag, tag, StringComparison.Ordinal) &&
            permissions.Contains(item.RequiredPermission));
}
