using HandballIntegration.Admin.Models;

namespace HandballIntegration.Admin.Abstractions;

public interface IAdminApiClient
{
    Task<AdminCapabilitiesResponse> GetCapabilitiesAsync(CancellationToken cancellationToken = default);
}

public interface IAdminCapabilitiesService
{
    IReadOnlySet<string> Current { get; }
    string ApiVersion { get; }
    Task RefreshAsync(CancellationToken cancellationToken = default);
    bool Has(string permission);
}

public interface IAdminSessionService
{
    AdminSession? Current { get; }
    bool IsAuthenticated { get; }
    bool IsExpired { get; }
    event EventHandler? SessionChanged;
    void Set(AdminSession session);
    void Clear();
}

public interface IAdminNavigationService
{
    IReadOnlyList<AdminNavigationItem> Build(IReadOnlySet<string> permissions);
    bool CanAccess(string tag, IReadOnlySet<string> permissions);
}

public interface IProblemDetailsMapper
{
    AdminClientError Map(System.Net.HttpStatusCode status, string? responseBody);
}

public interface IDialogService
{
    Task ShowErrorAsync(AdminClientError error);
    Task<bool> ConfirmAsync(string title, string message);
}

public interface IFilePickerService
{
    Task<string?> PickFileAsync(string filter, CancellationToken cancellationToken = default);
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface ICorrelationIdProvider
{
    string Create();
}

public interface IUiDispatcher
{
    Task InvokeAsync(Action action);
}

public interface INotificationService
{
    void Publish(string message, string severity, string? correlationId = null);
}

public interface IAdminSessionStorage
{
    AdminSession? Load();
    void Save(AdminSession session);
    void Clear();
}
