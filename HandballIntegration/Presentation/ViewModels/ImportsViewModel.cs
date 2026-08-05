using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Models;
using HandballIntegration.Core.Abstractions;
using HandballIntegration.Core.Models;
using HandballManagerCore.DTO;

namespace HandballIntegration.Presentation.ViewModels;

public sealed class ImportsViewModel : AdminPageViewModelBase
{
    private readonly IAdminImportApiClient _importClient;
    private readonly IAdminReferenceDataApiClient _referenceClient;
    private readonly IAdminCapabilitiesService _capabilities;
    private readonly IFilePickerService _filePicker;
    private string? _selectedFilePath;
    private string _fileName = "Aucun fichier selectionne";
    private string _fileSize = "--";
    private DateTime _matchDate = DateTime.Today;
    private string _season = CurrentSeason(DateTime.Today);
    private string _day = "J1";
    private CompetitionDto? _selectedCompetition;
    private AdminImportPreview? _preview;
    private AdminImportExecution? _execution;
    private string _reason = string.Empty;
    private string _confirmation = string.Empty;
    private int _currentStep = 1;

    public ImportsViewModel(
        IAdminImportApiClient importClient,
        IAdminReferenceDataApiClient referenceClient,
        IAdminCapabilitiesService capabilities,
        IFilePickerService filePicker) : base(
            "Integrations",
            "Un parcours controle de la source au rapport, sans ecriture avant confirmation.")
    {
        _importClient = importClient;
        _referenceClient = referenceClient;
        _capabilities = capabilities;
        _filePicker = filePicker;
        SelectFileCommand = new AsyncRelayCommand(SelectFileAsync, () => !IsLoading);
        PreviewCommand = new AsyncRelayCommand(PreviewAsync, CanPreview);
        ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
        ResetCommand = new RelayCommand(Reset);
    }

    public ObservableCollection<CompetitionDto> Competitions { get; } = [];
    public IAsyncRelayCommand SelectFileCommand { get; }
    public IAsyncRelayCommand PreviewCommand { get; }
    public IAsyncRelayCommand ExecuteCommand { get; }
    public IRelayCommand ResetCommand { get; }
    public IReadOnlyList<string> Seasons { get; } = Enumerable.Range(2010, 21).Select(year => $"{year}-{year + 1}").ToArray();
    public IReadOnlyList<string> Days { get; } = Enumerable.Range(1, 40).Select(day => $"J{day}").ToArray();

    public string FileName
    {
        get => _fileName;
        private set => SetProperty(ref _fileName, value);
    }

    public string FileSize
    {
        get => _fileSize;
        private set => SetProperty(ref _fileSize, value);
    }

    public DateTime MatchDate
    {
        get => _matchDate;
        set
        {
            SetProperty(ref _matchDate, value);
            PreviewCommand.NotifyCanExecuteChanged();
        }
    }

    public string Season
    {
        get => _season;
        set
        {
            SetProperty(ref _season, value);
            PreviewCommand.NotifyCanExecuteChanged();
        }
    }

    public string Day
    {
        get => _day;
        set
        {
            SetProperty(ref _day, value);
            PreviewCommand.NotifyCanExecuteChanged();
        }
    }

    public CompetitionDto? SelectedCompetition
    {
        get => _selectedCompetition;
        set
        {
            SetProperty(ref _selectedCompetition, value);
            PreviewCommand.NotifyCanExecuteChanged();
        }
    }

    public AdminImportPreview? Preview
    {
        get => _preview;
        private set
        {
            SetProperty(ref _preview, value);
            ExecuteCommand.NotifyCanExecuteChanged();
        }
    }

    public AdminImportExecution? Execution
    {
        get => _execution;
        private set => SetProperty(ref _execution, value);
    }

    public string Reason
    {
        get => _reason;
        set
        {
            SetProperty(ref _reason, value);
            ExecuteCommand.NotifyCanExecuteChanged();
        }
    }

    public string Confirmation
    {
        get => _confirmation;
        set
        {
            SetProperty(ref _confirmation, value);
            ExecuteCommand.NotifyCanExecuteChanged();
        }
    }

    public int CurrentStep
    {
        get => _currentStep;
        private set => SetProperty(ref _currentStep, value);
    }

    public bool HasPreview => Preview is not null;
    public bool HasExecution => Execution is not null;

    protected override async Task<AdminPageState> LoadAsync(CancellationToken cancellationToken)
    {
        var competitions = await _referenceClient.GetCompetitionsAsync(cancellationToken);
        Competitions.Clear();
        foreach (var competition in competitions)
        {
            Competitions.Add(competition);
        }

        SelectedCompetition ??= Competitions.FirstOrDefault();
        return Competitions.Count == 0
            ? AdminPageState.Partial("Aucune competition n'est disponible pour construire le contexte d'import.")
            : AdminPageState.Loaded("Selectionnez une source XLSX pour commencer.");
    }

    private async Task SelectFileAsync()
    {
        var filePath = await _filePicker.PickFileAsync("Classeur Excel (*.xlsx)|*.xlsx");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var file = new FileInfo(filePath);
        _selectedFilePath = file.FullName;
        FileName = file.Name;
        FileSize = FormatBytes(file.Length);
        Preview = null;
        Execution = null;
        CurrentStep = 1;
        State = AdminPageState.Idle("Source selectionnee. Verifiez maintenant son contexte.");
        PreviewCommand.NotifyCanExecuteChanged();
    }

    private bool CanPreview() =>
        !IsLoading &&
        _capabilities.Has(AdminPermissionNames.ImportsPreview) &&
        !string.IsNullOrWhiteSpace(_selectedFilePath) &&
        SelectedCompetition is not null &&
        !string.IsNullOrWhiteSpace(Season) &&
        !string.IsNullOrWhiteSpace(Day);

    private async Task PreviewAsync()
    {
        if (!CanPreview() || _selectedFilePath is null || SelectedCompetition is null)
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            Preview = await _importClient.PreviewAsync(
                _selectedFilePath,
                MatchDate,
                Season,
                Day,
                SelectedCompetition.CompetitionId,
                cancellationToken);
            CurrentStep = 4;
            OnPropertyChanged(nameof(HasPreview));
            return Preview.BlockingIssues.Count > 0
                ? AdminPageState.Partial("La previsualisation contient des erreurs bloquantes a corriger.")
                : AdminPageState.Loaded("Previsualisation terminee. Aucune donnee n'a ete ecrite.");
        });
    }

    private bool CanExecute() =>
        !IsLoading &&
        _capabilities.Has(AdminPermissionNames.ImportsExecute) &&
        Preview is { CanExecute: true } &&
        Preview.ExpiresAtUtc > DateTime.UtcNow &&
        !string.IsNullOrWhiteSpace(Reason) &&
        string.Equals(Confirmation.Trim(), "INTEGRER", StringComparison.Ordinal);

    private async Task ExecuteAsync()
    {
        if (!CanExecute() || Preview is null)
        {
            return;
        }

        await RunCommandAsync(async cancellationToken =>
        {
            var request = new AdminImportExecutionRequest(
                Preview.Sha256,
                Preview.MappingVersion,
                Guid.NewGuid().ToString("N"),
                Reason.Trim(),
                true,
                Preview.ExpectedSummary);
            Execution = await _importClient.ExecuteAsync(Preview.PreviewId, request, cancellationToken);
            CurrentStep = 7;
            OnPropertyChanged(nameof(HasExecution));
            return AdminPageState.Loaded("Import termine. Le rapport transactionnel est disponible.");
        });
    }

    private void Reset()
    {
        _selectedFilePath = null;
        FileName = "Aucun fichier selectionne";
        FileSize = "--";
        Preview = null;
        Execution = null;
        Reason = string.Empty;
        Confirmation = string.Empty;
        CurrentStep = 1;
        State = AdminPageState.Idle("Selectionnez une source XLSX pour commencer.");
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(HasExecution));
        PreviewCommand.NotifyCanExecuteChanged();
        ExecuteCommand.NotifyCanExecuteChanged();
    }

    private static string CurrentSeason(DateTime date) =>
        date.Month >= 7 ? $"{date.Year}-{date.Year + 1}" : $"{date.Year - 1}-{date.Year}";

    private static string FormatBytes(long length) =>
        length >= 1024 * 1024
            ? $"{length / 1024d / 1024d:0.0} Mo"
            : $"{Math.Max(1, length / 1024d):0} Ko";
}
