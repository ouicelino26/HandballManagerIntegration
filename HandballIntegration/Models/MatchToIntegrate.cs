using CommunityToolkit.Mvvm.ComponentModel;
using HandballManagerCore.DTO;
using System;

public partial class MatchToIntegrate : ObservableObject
{
    [ObservableProperty]
    private string fileName;

    [ObservableProperty]
    private string fullPath;

    [ObservableProperty]
    private MatchDto matchInfo;

    [ObservableProperty]
    private IntegrationStatus status = IntegrationStatus.Pending;

    [ObservableProperty]
    private string statusMessage = "En attente";

    [ObservableProperty]
    private bool isBusy;
}

public enum IntegrationStatus
{
    Pending,
    Converting,
    Integrating,
    Success,
    Error
}
