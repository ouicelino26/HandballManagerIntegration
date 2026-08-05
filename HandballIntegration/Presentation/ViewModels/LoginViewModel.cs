using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandballIntegration.Data;
using HandballIntegration.Services;

namespace HandballIntegration.Presentation.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private readonly IApiAuthService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public LoginViewModel(IApiAuthService authService, ApiSettings settings)
    {
        _authService = authService;
        Environment = settings.EnvironmentLabel;
        LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
    }

    public event EventHandler? LoginSucceeded;
    public IAsyncRelayCommand LoginCommand { get; }
    public string Environment { get; }

    public string Username
    {
        get => _username;
        set
        {
            SetProperty(ref _username, value);
            LoginCommand.NotifyCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            LoginCommand.NotifyCanExecuteChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            SetProperty(ref _errorMessage, value);
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            SetProperty(ref _isBusy, value);
            OnPropertyChanged(nameof(IsFormEnabled));
            LoginCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsFormEnabled => !IsBusy;

    private bool CanLogin() =>
        !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(Username.Trim(), Password);
            Password = string.Empty;
            if (!result.Success)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "La connexion a ete refusee. Verifiez vos informations puis reessayez."
                    : result.Message;
                return;
            }

            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            Password = string.Empty;
            ErrorMessage = "La connexion est momentanement indisponible. Reessayez ou contactez le support.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
