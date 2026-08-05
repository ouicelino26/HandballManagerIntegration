using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandballIntegration.Admin.Models;
using HandballIntegration.Core.Models;

namespace HandballIntegration.Presentation.ViewModels;

public interface IAdminModuleViewModel
{
    string Title { get; }
    string Subtitle { get; }
    AdminPageState State { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public abstract class AdminPageViewModelBase : ObservableObject, IAdminModuleViewModel, IDisposable
{
    private CancellationTokenSource? _requestSource;
    private long _requestVersion;
    private AdminPageState _state = AdminPageState.Idle();

    protected AdminPageViewModelBase(string title, string subtitle)
    {
        Title = title;
        Subtitle = subtitle;
        RefreshCommand = new AsyncRelayCommand(
            cancellationToken => RefreshAsync(cancellationToken),
            () => !IsLoading);
    }

    public string Title { get; }
    public string Subtitle { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public AdminPageState State
    {
        get => _state;
        protected set
        {
            if (!SetProperty(ref _state, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(HasBlockingState));
            RefreshCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsLoading => State.Kind == AdminPageStateKind.Loading;
    public bool HasContent => State.Kind is AdminPageStateKind.Loaded or AdminPageStateKind.Partial;
    public bool HasBlockingState => State.Kind is AdminPageStateKind.Empty or AdminPageStateKind.Error or
        AdminPageStateKind.Forbidden or AdminPageStateKind.Conflict or AdminPageStateKind.Offline or
        AdminPageStateKind.Cancelled;

    public virtual Task InitializeAsync(CancellationToken cancellationToken = default) =>
        RefreshAsync(cancellationToken);

    protected abstract Task<AdminPageState> LoadAsync(CancellationToken cancellationToken);

    protected Task RefreshAsync() => RefreshAsync(CancellationToken.None);

    protected async Task RefreshAsync(CancellationToken cancellationToken)
    {
        _requestSource?.Cancel();
        _requestSource?.Dispose();
        _requestSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var version = Interlocked.Increment(ref _requestVersion);
        State = AdminPageState.Loading();

        try
        {
            var next = await LoadAsync(_requestSource.Token);
            if (version == _requestVersion)
            {
                State = next;
            }
        }
        catch (OperationCanceledException) when (_requestSource.IsCancellationRequested)
        {
            if (version == _requestVersion)
            {
                State = AdminPageState.Cancelled();
            }
        }
        catch (AdminApiException exception)
        {
            if (version == _requestVersion)
            {
                State = AdminPageState.FromError(
                    exception.Error.Status,
                    exception.Error.Code,
                    exception.Error.Message,
                    exception.Error.CorrelationId);
            }
        }
        catch (Exception)
        {
            if (version == _requestVersion)
            {
                State = AdminPageState.FromError(
                    HttpStatusCode.InternalServerError,
                    "ADMIN_CLIENT_ERROR",
                    "Une erreur inattendue empeche l'affichage de ce module.",
                    null);
            }
        }
    }

    protected async Task<AdminPageState> RunCommandAsync(
        Func<CancellationToken, Task<AdminPageState>> action,
        CancellationToken cancellationToken = default)
    {
        State = AdminPageState.Loading("Operation en cours");
        try
        {
            var next = await action(cancellationToken);
            State = next;
            return next;
        }
        catch (OperationCanceledException)
        {
            State = AdminPageState.Cancelled();
            return State;
        }
        catch (AdminApiException exception)
        {
            State = AdminPageState.FromError(
                exception.Error.Status,
                exception.Error.Code,
                exception.Error.Message,
                exception.Error.CorrelationId);
            return State;
        }
        catch (Exception)
        {
            State = AdminPageState.FromError(
                HttpStatusCode.InternalServerError,
                "ADMIN_CLIENT_ERROR",
                "Une erreur inattendue empeche cette operation.",
                null);
            return State;
        }
    }

    public void Dispose()
    {
        _requestSource?.Cancel();
        _requestSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public sealed class BlockedModuleViewModel : AdminPageViewModelBase
{
    public BlockedModuleViewModel(
        string title,
        string subtitle,
        AdminApiAvailability availability) : base(title, subtitle)
    {
        Availability = availability;
    }

    public AdminApiAvailability Availability { get; }

    protected override Task<AdminPageState> LoadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(AdminPageState.Partial(Availability.Message));
}
