using CommunityToolkit.Mvvm.ComponentModel;
using HandballManagerCore.DTO;

public partial class TimePlayersFileToIntegrate : ObservableObject
{
    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private string fullPath = string.Empty;

    [ObservableProperty]
    private string teamsLabel = string.Empty;

    [ObservableProperty]
    private MatchDto matchInfo = new();

    [ObservableProperty]
    private IntegrationStatus status = IntegrationStatus.Pending;

    [ObservableProperty]
    private string statusMessage = "En attente";

    [ObservableProperty]
    private bool isBusy;
}
