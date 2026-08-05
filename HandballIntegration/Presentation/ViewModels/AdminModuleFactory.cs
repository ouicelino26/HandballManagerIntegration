using HandballIntegration.Core.Abstractions;
using HandballIntegration.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HandballIntegration.Presentation.ViewModels;

public interface IAdminModuleFactory
{
    IAdminModuleViewModel Create(string tag);
}

public sealed class AdminModuleFactory(IServiceProvider services) : IAdminModuleFactory
{
    public IAdminModuleViewModel Create(string tag) => tag switch
    {
        "dashboard" => services.GetRequiredService<DashboardViewModel>(),
        "integration" => services.GetRequiredService<LegacyIntegrationViewModel>(),
        "matches" or "events" => services.GetRequiredService<MatchesViewModel>(),
        "players" => services.GetRequiredService<PlayersAdminViewModel>(),
        "teams" => services.GetRequiredService<TeamsAdminViewModel>(),
        "reference-data" => services.GetRequiredService<ReferenceDataViewModel>(),
        "audit" => services.GetRequiredService<AuditViewModel>(),
        "maintenance" or "settings" => services.GetRequiredService<SettingsViewModel>(),
        "users" => services.GetRequiredService<UsersAdminViewModel>(),
        "data-quality" => Blocked(
            "Qualite des donnees",
            "Les anomalies seront actionnables quand le contrat serveur sera disponible.",
            services.GetRequiredService<IAdminDataQualityApiClient>().Availability),
        "reconciliation" => Blocked(
            "Reconciliation",
            "Aucune association ambigue n'est automatisee.",
            new AdminApiAvailability(
                false,
                "BLOCKED_BY_API",
                "GET/POST /api/v2/admin/reconciliation",
                "Les files de reconciliation et leur resolution auditee ne sont pas exposees.")),
        "import-history" => Blocked(
            "Historique des imports",
            "Les anciens imports ne sont jamais reexecutes automatiquement.",
            new AdminApiAvailability(
                false,
                "BLOCKED_BY_API",
                "GET /api/v2/admin/imports",
                "L'API execute les previews mais ne fournit pas encore la liste des executions.")),
        _ => Blocked(
            "Module indisponible",
            "Le module demande n'est pas configure.",
            new AdminApiAvailability(false, "BLOCKED", "N/A", "Aucun contrat n'est associe a cette navigation."))
    };

    private static BlockedModuleViewModel Blocked(
        string title,
        string subtitle,
        AdminApiAvailability availability) => new(title, subtitle, availability);
}
