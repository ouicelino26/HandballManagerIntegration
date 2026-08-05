using HandballIntegration.Admin.Models;

namespace HandballIntegration.Admin.Workflows;

public static class AdminStartupDecision
{
    public static bool ShouldShutdownAfterLogin(bool? loginResult) => loginResult != true;
}

public sealed record AdminShellState(
    string ApplicationName,
    string Environment,
    string ApiStatus,
    string ClientVersion,
    string ApiVersion);

public sealed record AdminPermissionViewState(bool ShowContent, bool ShowPermissionDenied)
{
    public static AdminPermissionViewState FromCapability(bool allowed) =>
        new(allowed, !allowed);
}

public sealed record AdminErrorState(
    string Code,
    string? CorrelationId,
    DateTime OccurredAtUtc,
    string Action)
{
    public static AdminErrorState From(AdminClientError error, DateTime occurredAtUtc) =>
        new(error.Code, error.CorrelationId, occurredAtUtc, error.Action);
}

public sealed class AdminImportWorkflow
{
    public string? PreviewId { get; private set; }
    public bool CanExecute { get; private set; }
    public bool IsConfirmed { get; private set; }

    public void LoadPreview(string previewId, bool canExecute)
    {
        PreviewId = previewId;
        CanExecute = canExecute;
        IsConfirmed = false;
    }

    public void Confirm() => IsConfirmed = true;

    public bool TryBeginExecute() =>
        !string.IsNullOrWhiteSpace(PreviewId) && CanExecute && IsConfirmed;
}

public sealed class AdminDeletionGuard
{
    public bool ImpactLoaded { get; private set; }
    public string? ConfirmationToken { get; private set; }
    public IReadOnlyList<string> Dependencies { get; private set; } = [];

    public void LoadImpact(string confirmationToken, IEnumerable<string>? dependencies = null)
    {
        ImpactLoaded = true;
        ConfirmationToken = confirmationToken;
        Dependencies = dependencies?.ToArray() ?? [];
    }

    public bool CanSubmit(string? reason) =>
        ImpactLoaded &&
        !string.IsNullOrWhiteSpace(ConfirmationToken) &&
        !string.IsNullOrWhiteSpace(reason);
}

public sealed class AdminConcurrencyChoice
{
    public IReadOnlyList<string> Actions { get; } =
        ["Actualiser", "Comparer", "Abandonner", "Reappliquer manuellement"];
}

public sealed class CancellableLoadingState
{
    private CancellationTokenSource? _source;
    public bool IsLoading { get; private set; }

    public CancellationToken Begin()
    {
        _source?.Cancel();
        _source?.Dispose();
        _source = new CancellationTokenSource();
        IsLoading = true;
        return _source.Token;
    }

    public void Cancel()
    {
        _source?.Cancel();
        IsLoading = false;
    }
}
