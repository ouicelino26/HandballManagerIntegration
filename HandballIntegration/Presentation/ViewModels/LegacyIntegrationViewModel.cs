using HandballIntegration.Core.Models;
using HandballIntegration.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HandballIntegration.Presentation.ViewModels;

public sealed class LegacyIntegrationViewModel : IAdminModuleViewModel
{
    public LegacyIntegrationViewModel(IServiceProvider services)
    {
        ClassicViewModel = services.GetRequiredService<IntegrationViewModel>();
        TimeViewModel = services.GetRequiredService<TimeIntegrationViewModel>();
    }

    public IntegrationViewModel ClassicViewModel { get; }
    public TimeIntegrationViewModel TimeViewModel { get; }

    public string Title => "Integration";
    public string Subtitle => "Import des actions et des temps de jeu depuis les fichiers du match.";
    public AdminPageState State => AdminPageState.Loaded();

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
