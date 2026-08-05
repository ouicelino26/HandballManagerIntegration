using HandballIntegration.Admin.Abstractions;

namespace HandballIntegration.Admin.Services;

public sealed class AdminCapabilitiesService(IAdminApiClient apiClient) : IAdminCapabilitiesService
{
    private IReadOnlySet<string> _current = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlySet<string> Current => _current;
    public string ApiVersion { get; private set; } = "Indisponible";

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _current = new HashSet<string>(StringComparer.Ordinal);
        ApiVersion = "Indisponible";
        var response = await apiClient.GetCapabilitiesAsync(cancellationToken);
        _current = response.Capabilities
            .Where(item => item.Allowed)
            .Select(item => item.Name)
            .ToHashSet(StringComparer.Ordinal);
        ApiVersion = response.ApiVersion;
    }

    public bool Has(string permission) => _current.Contains(permission);
}
