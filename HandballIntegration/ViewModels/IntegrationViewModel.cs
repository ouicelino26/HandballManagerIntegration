using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanballManagerMaui.Services;
using HandballIntegration.Converters;
using HandballManagerCore.DTO;
using HandballManagerCore.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public partial class IntegrationViewModel : ObservableObject
{
    private readonly XlsxToCsvConverter _converter = new();
    private readonly MatchFileImportService _importService = new();
    private readonly HttpClient _http;
    public IntegrationViewModel(HttpClient httpClient)
    {
        _http = httpClient;         
        IntegrateCommand = new AsyncRelayCommand<MatchToIntegrate>(IntegrateFileAsync);
    }

    [ObservableProperty]
    private string selectedFolder;

    public ObservableCollection<MatchToIntegrate> Files { get; set; }
        = new ObservableCollection<MatchToIntegrate>();

    public IRelayCommand<MatchToIntegrate> IntegrateCommand { get; }

    public IntegrationViewModel()
    {
        IntegrateCommand = new AsyncRelayCommand<MatchToIntegrate>(IntegrateFileAsync);
    }


    // -------------------------------------------------------
    // Charger les fichiers XLSX
    // -------------------------------------------------------
    public void LoadFiles(string folderPath)
    {
        SelectedFolder = folderPath;
        Files.Clear();

        var filePaths = Directory.GetFiles(folderPath, "*.xlsx", SearchOption.AllDirectories);

        foreach (var file in filePaths)
        {
            string fileName = Path.GetFileName(file);

            if (fileName.StartsWith("Table historique des actions du match",
                                    StringComparison.OrdinalIgnoreCase))
            {
                Files.Add(new MatchToIntegrate
                {
                    FileName = fileName,
                    FullPath = file,
                    MatchInfo = new MatchDto
                    {
                        Date = DateTime.Today
                    }
                });
            }
        }
    }


    // -------------------------------------------------------
    // Intégration 1 fichier
    // -------------------------------------------------------
    public async Task IntegrateFileAsync(MatchToIntegrate file)
    {
        try
        {
            file.IsBusy = true;
            file.Status = IntegrationStatus.Converting;
            file.StatusMessage = "Conversion XLSX...";

            // 1️⃣ Conversion XLSX → CSV
            string csvPath = _converter.ConvertXlsxToCsv(file.FullPath);

            // 2️⃣ Import CSV
            file.StatusMessage = "Lecture CSV...";
            var rows = _importService.ImportFromCsv(csvPath);
            if (rows.Count == 0)
                throw new Exception("Le fichier CSV est vide.");

            // 3️⃣ Identifier équipes
            file.StatusMessage = "Recherche équipes...";
            var teamNames = rows.Select(r => (r.TeamId ?? "").Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Distinct()
                                .Take(2)
                                .ToList();

            if (teamNames.Count < 2)
                throw new Exception("Impossible d'identifier deux équipes.");

            int? team1Id = await ApiResolveTeamId(teamNames[0]);
            int? team2Id = await ApiResolveTeamId(teamNames[1]);

            if (team1Id == null || team2Id == null)
                throw new Exception("Équipe introuvable.");

            // 4️⃣ Créer match
            file.Status = IntegrationStatus.Integrating;
            file.StatusMessage = "Création du match...";

            var last = rows.Last();

            var newMatch = new Match
            {
                CompetitionId = 1,
                Date = file.MatchInfo.Date,
                Team1Id = team1Id.Value,
                Team2Id = team2Id.Value,
                Team1Score = TryParseInt(last.TeamScore1) ?? 0,
                Team2Score = TryParseInt(last.TeamScore2) ?? 0,
                Year = file.MatchInfo.Date.Year
            };

            var respMatch = await _http.PostAsJsonAsync("api/Matches", newMatch);
            respMatch.EnsureSuccessStatusCode();

            var createdMatch = await respMatch.Content.ReadFromJsonAsync<Match>();

            // 5️⃣ Enregistrer événements
            file.StatusMessage = "Importation événements...";

            foreach (var dto in rows)
            {
                int? teamId = await ApiResolveTeamId(dto.TeamId);
                int? playerId = await ApiResolvePlayerId(dto.PlayerId);
                int? eventId = await ApiResolveId("api/Events/byname/", dto.EventId);
                int? attackId = await ApiResolveId("api/Attacks/byname/", dto.AttackId);
                int? defenseId = await ApiResolveId("api/Defenses/byname/", dto.DefenseId);

                var matchEvent = new MatchEvent
                {
                    MatchId = createdMatch.Id,
                    TeamId = teamId ?? 0,
                    PlayerId = playerId ?? 0,
                    EventId = eventId ?? 0,
                    AttackId = attackId,
                    DefenseId = defenseId,
                    Time = ParseTime(dto.Time),
                    TeamScore1 = TryParseInt(dto.TeamScore1),
                    TeamScore2 = TryParseInt(dto.TeamScore2),
                    ShootShade = dto.ShootShade,
                    Trigger = dto.Trigger
                };

                var resp = await _http.PostAsJsonAsync("api/MatchEvents", matchEvent);
                resp.EnsureSuccessStatusCode();
            }

            // 6️⃣ OK
            file.IsBusy = false;
            file.Status = IntegrationStatus.Success;
            file.StatusMessage = "Fichier intégré ✔";
        }
        catch (Exception ex)
        {
            file.IsBusy = false;
            file.Status = IntegrationStatus.Error;
            file.StatusMessage = "Erreur : " + ex.Message;
        }
    }

    // ---------------- HELPERS -------------------
    private async Task<int?> ApiResolveTeamId(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var resp = await _http.GetAsync($"api/Teams/byname/{Uri.EscapeDataString(name)}");
        if (!resp.IsSuccessStatusCode) return null;
        return (await resp.Content.ReadFromJsonAsync<Team>())?.Id;
    }

    private async Task<int?> ApiResolvePlayerId(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var resp = await _http.GetAsync($"api/Players/byfullname/{Uri.EscapeDataString(name)}");
        if (!resp.IsSuccessStatusCode) return null;
        return (await resp.Content.ReadFromJsonAsync<Player>())?.Id;
    }

    private async Task<int?> ApiResolveId(string route, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var resp = await _http.GetAsync($"{route}{Uri.EscapeDataString(name)}");
        if (!resp.IsSuccessStatusCode) return null;
        return (await resp.Content.ReadFromJsonAsync<IdNameDto>())?.Id;
    }

    private static int? TryParseInt(string? s)
        => int.TryParse((s ?? "").Trim(), out var v) ? v : null;

    private static TimeSpan? ParseTime(string? s)
        => TimeSpan.TryParse(s, out var t) ? t : null;

    private sealed class IdNameDto { public int Id { get; set; } }
}
