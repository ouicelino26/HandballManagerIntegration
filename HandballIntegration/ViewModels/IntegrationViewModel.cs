using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HanballManagerMaui.Services;
using HandballIntegration;
using HandballIntegration.Converters;
using HandballIntegration.Data;
using HandballIntegration.Services;
using HandballIntegration.Views;
using HandballManagerCore.DTO;
using HandballManagerCore.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Windows.Web.Http;

public partial class IntegrationViewModel : ObservableObject
{
    private readonly XlsxToCsvConverter _converter = new();
    private readonly MatchFileImportService _importService = new();
    private readonly System.Net.Http.HttpClient _http;
    private readonly ApiService _apiService;
    private readonly ApiSettings _settings;
    private static string Key(string s)
    => (s ?? "").Trim().ToLower();
    Dictionary<string, string> _playerNameMap = new();

    public List<string> JoursDisponibles { get; } =
    Enumerable.Range(1, 28)
              .Select(i => $"J{i}")
              .ToList();

    public IntegrationViewModel()
    {

        _apiService = App.Services.GetRequiredService<ApiService>();
        _http = App.Services.GetRequiredService<System.Net.Http.HttpClient>();

        var options = App.Services.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<ApiSettings>>();

        _settings = options.Value;
        IntegrateCommand = new AsyncRelayCommand<MatchToIntegrate>(IntegrateFileAsync);
        if (_settings == null)
        {
            Console.WriteLine("vide");

        }
        else if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            Console.WriteLine("vide");
        }
    }



    [ObservableProperty]
    private string selectedFolder;

    public ObservableCollection<MatchToIntegrate> Files { get; set; }
        = new ObservableCollection<MatchToIntegrate>();

    public IRelayCommand<MatchToIntegrate> IntegrateCommand { get; }

  

    public void LoadFiles(string folderPath)
    {
        try
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
                    var detectedDay = ExtractMatchDay(file);

                    Files.Add(new MatchToIntegrate
                    {
                        FileName = fileName,
                        FullPath = file,
                        MatchInfo = new MatchDto
                        {
                            Date = DateTime.Today,
                            Day = detectedDay ?? JoursDisponibles.First()
                        }
                    });
                }
            }
        }
        catch
        {

        }
    }


    // -------------------------------------------------------
    // Intégration 1 fichier
    // -------------------------------------------------------
    public async Task IntegrateFileAsync(MatchToIntegrate file)
    {
        file.IsBusy = true;
        file.Status = IntegrationStatus.Converting;
        file.StatusMessage = "Conversion XLSX...";

        try
        {
            await Task.Run(async () =>
            {
                await IntegrateInternalAsync(file);
            });

            file.Status = IntegrationStatus.Success;
            file.StatusMessage = "Fichier intégré ✔";
        }
        catch (Exception ex)
        {
            //var personne = dto.PlayerId;
            file.Status = IntegrationStatus.Error;
            file.StatusMessage = "Erreur : " + ex.Message;
        }
        finally
        {
            file.IsBusy = false;
        }
    }

    public async Task IntegrateInternalAsync(MatchToIntegrate file)
    {
        if (!await _apiService.PrepareAuthorizedClientAsync(_http))
        {
            throw new Exception("Session administrateur requise.");
        }

        // Conversion XLSX → CSV
        string csvPath = _converter.ConvertXlsxToCsv(file.FullPath);

        // Import CSV
        file.StatusMessage = "Lecture CSV...";
        var rows = _importService.ImportFromCsv(csvPath);
        if (rows.Count == 0)
            throw new Exception("Le fichier CSV est vide.");


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

        var teamsFromFile = new List<TeamLight>
            {
                new TeamLight { Id = team1Id.Value, Name = teamNames[0] },
                new TeamLight { Id = team2Id.Value, Name = teamNames[1] }
            };


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
            Year = file.MatchInfo.Date.Year,
            Day = file.MatchInfo.Day
        };

        var respMatch = await _http.PostAsJsonAsync($"{_settings.BaseUrl}api/Matches", newMatch);
        respMatch.EnsureSuccessStatusCode();

        var createdMatch = await respMatch.Content.ReadFromJsonAsync<Match>();


        file.StatusMessage = "Importation événements...";
        int currentHalf = 1;
        TimeSpan? prevTime = null;

        for (int i = 0; i < rows.Count; i++)
        {
            var dto = rows[i];
            int rowIndex = i + 2; // +1 header, +1 for 1-based row index
            var parsedTime = ParseTime(dto.Time);
            var prevTimeForLog = prevTime;
            var miTemps = NormalizeMiTemps(dto.MiTemps);
            bool resetDetected = false;

            if (string.IsNullOrWhiteSpace(miTemps))
            {
                if (prevTimeForLog != null && parsedTime != null && parsedTime.Value < prevTimeForLog.Value)
                {
                    currentHalf++;
                    resetDetected = true;
                    LogHalfReset(rowIndex, prevTimeForLog.Value, parsedTime.Value, dto);
                }

                miTemps = $"MT{currentHalf}";
            }
            else
            {
                if (miTemps == "MT1") currentHalf = 1;
                else if (miTemps == "MT2") currentHalf = Math.Max(currentHalf, 2);
            }

            dto.MiTemps = miTemps;

            if (parsedTime != null)
                prevTime = parsedTime;

            bool isBoundary = resetDetected
                || IsNearHalfBoundary(parsedTime)
                || IsNearHalfBoundary(prevTimeForLog);

            if (dto.Number >= 100)
            {
                LogSkip("Number>=100", rowIndex, dto, parsedTime, miTemps, prevTimeForLog, resetDetected, isBoundary);
                continue; // on integre pas l'évenement 
            }
            int? teamId = await ApiResolveTeamId(dto.TeamId);
            string originalName = dto.PlayerId;
            var key = Key(originalName);
            if (_playerNameMap.TryGetValue(key, out var mappedName))
            {

                dto.PlayerId = mappedName;
            }

            int? playerId = await ApiResolvePlayerId(dto.PlayerId);


            if (playerId == null && !string.IsNullOrWhiteSpace(dto.PlayerId))
            {
                var parts = dto.PlayerId.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string prenom = parts.Length > 0 ? parts[0] : "";
                string nom = parts.Length > 1 ? parts[^1] : "";

                var candidates = await ApiSearchPlayersApprox(dto.PlayerId);

                if (!candidates.Any())
                {
                    if (!string.IsNullOrEmpty(prenom))
                        candidates.AddRange(await ApiSearchPlayersApprox(prenom));

                    if (!string.IsNullOrEmpty(nom))
                        candidates.AddRange(await ApiSearchPlayersApprox(nom));
                }

                candidates = candidates
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .ToList();

                Player? selected = null;

                if (candidates.Any())
                {
                    selected = TryAutoSelectPlayerCandidate(candidates, originalName);

                    if (selected != null)
                    {
                        LogSimple($"Player auto-selected '{originalName}' -> '{selected.FullName}'");
                    }
                    else
                    {
                        selected = await ShowPlayerSelectionAsync(candidates, originalName);
                    }
                }

               
                if (selected == null)
                {
                    var createdPlayer = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var win = new AddPlayerWindows(prenom, nom, teamsFromFile);
                        win.Owner = System.Windows.Application.Current.MainWindow;
                        return win.ShowDialog() == true ? win.CreatedPlayer : null;
                    });

                    if (createdPlayer == null)
                        continue;

                    playerId = createdPlayer.Id;
                    _playerNameMap[key] = createdPlayer.FullName;
                }
                else
                {

                    playerId = selected.Id;
                    _playerNameMap[key] = selected.FullName;
                }
            }
            int? eventId = await ApiResolveIdEvent($"{_settings.BaseUrl}api/Event/", dto.EventId);
            int? attackId = await ApiResolveIdAttack(dto.AttackId);
            int? defenseId = await ApiResolveIdDefense(dto.DefenseId);
            if (teamId == null)
            {
                LogSkip("Team inconnue", rowIndex, dto, parsedTime, miTemps, prevTimeForLog, resetDetected, isBoundary);
                continue;
            }

            if (playerId == null)
            {
                LogSkip("Player inconnu", rowIndex, dto, parsedTime, miTemps, prevTimeForLog, resetDetected, isBoundary);
                continue;
            }

            // ---------- EVENT INCONNU = ID 37 ----------
            if (eventId == null)
            {
                LogSimple($"Event inconnu '{dto.EventId}' → forcé à ID 37");
                eventId = 37;
            }

            var matchEvent = new MatchEvent
            {


                MatchId = createdMatch.Id,
                TeamId = teamId ?? 0,
                PlayerId = playerId ?? 0,
                EventId = eventId.Value,
                AttackId = attackId,
                DefenseId = defenseId,
                Time = ParseTime(dto.Time),
                MiTemps = miTemps,
                TeamScore1 = TryParseInt(dto.TeamScore1),
                TeamScore2 = TryParseInt(dto.TeamScore2),
                Action = dto.Action,
                ShootZone = dto.ShootZone,
                Shade = dto.Shade,
                ShootShade = dto.ShootShade,
                ArmSide = dto.ArmSide,
                Jump = dto.Jump,
                Trigger = dto.Trigger,
                PlayerNumber1 = dto.PlayerNumber1,
                PlayerNumber2 = dto.PlayerNumber2
            };

            var resp = await _http.PostAsJsonAsync($"{_settings.BaseUrl}api/MatchEvents", matchEvent);


            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();

                var sentJson = System.Text.Json.JsonSerializer.Serialize(
                    matchEvent,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                var log =
            $"""
                    ==============================
                    {DateTime.Now}
                    
                    ROUTE : POST api/MatchEvents
                    STATUS : {(int)resp.StatusCode} {resp.ReasonPhrase}
                    
                    ----- OBJET ENVOYÉ -----
                    {sentJson}
                    
                    ----- RÉPONSE API -----
                    {body}
                    
                    ==============================
                    
                    """;

                File.AppendAllText("integration_errors.log", log);

                continue; // on passe à l’event suivant
            }

            resp.EnsureSuccessStatusCode();


        
        }
    }
       

    // ---------------- HELPERS -------------------
    private async Task<int?> ApiResolveTeamId(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var resp = await _http.GetAsync($"{_settings.BaseUrl}teams/byname/{Uri.EscapeDataString(name)}");
        
        if (!resp.IsSuccessStatusCode) return null;
        return (await resp.Content.ReadFromJsonAsync<Team>())?.Id;
    }
    public async Task<int?> ApiResolveIdEvent(string baseUrl, string name)
    {
        var encoded = Uri.EscapeDataString(name);
        var url = $"{baseUrl}{encoded}";

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var events = await response.Content.ReadFromJsonAsync<List<Event>>();

        return events?.FirstOrDefault()?.Id;
    }
    public async Task<int?> ApiResolveIdAttack(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        foreach (var baseUrl in new[]
        {
            $"{_settings.BaseUrl}api/Attacks/byname/",
            $"{_settings.BaseUrl}api/Attacks/"
        })
        {
            var encoded = Uri.EscapeDataString(name);
            var response = await _http.GetAsync($"{baseUrl}{encoded}");
            if (!response.IsSuccessStatusCode)
                continue;

            var attacks = await response.Content.ReadFromJsonAsync<List<Attack>>();
            var attackId = attacks?.FirstOrDefault()?.Id;
            if (attackId != null)
                return attackId;
        }

        return null;
    }
    public async Task<int?> ApiResolveIdDefense(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        foreach (var baseUrl in new[]
        {
            $"{_settings.BaseUrl}api/Defenses/byname/",
            $"{_settings.BaseUrl}api/Defenses/"
        })
        {
            var encoded = Uri.EscapeDataString(name);
            var response = await _http.GetAsync($"{baseUrl}{encoded}");
            if (!response.IsSuccessStatusCode)
                continue;

            var defenses = await response.Content.ReadFromJsonAsync<List<Defense>>();
            var defenseId = defenses?.FirstOrDefault()?.Id;
            if (defenseId != null)
                return defenseId;
        }

        return null;
    }
    private async Task<int?> ApiResolvePlayerId(string? name)
    {
        
        if (string.IsNullOrWhiteSpace(name)) return null;

        var resp = await _http.GetAsync($"{_settings.BaseUrl}api/Players/byfullname/{Uri.EscapeDataString(name)}");

        if (!resp.IsSuccessStatusCode) return null;
        return (await resp.Content.ReadFromJsonAsync<Player>())?.Id;
    }
    private async Task<List<Player>> ApiSearchPlayersApprox(string name)
    {
        var resp = await _http.GetAsync(
            $"{_settings.BaseUrl}api/Players/search/{Uri.EscapeDataString(name)}");

        if (!resp.IsSuccessStatusCode)
            return new List<Player>();

        return await resp.Content.ReadFromJsonAsync<List<Player>>()
               ?? new List<Player>();
    }
    private Task<Player?> ShowPlayerSelectionAsync(List<Player> candidates, string searchedName)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = new FoundPlayersWindows(candidates, searchedName);
            window.Owner = System.Windows.Application.Current.MainWindow;

            bool? result = window.ShowDialog();
            return result == true ? window.SelectedPlayer : null;
        }).Task;
    }

    private Player? TryAutoSelectPlayerCandidate(List<Player> candidates, string searchedName)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        if (candidates.Count == 1)
            return candidates[0];

        var ranked = candidates
            .Select(candidate => new
            {
                Player = candidate,
                Score = ScorePlayerCandidate(candidate.FullName, searchedName)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => NormalizeNameForCompare(x.Player.FullName).Length)
            .ToList();

        if (ranked.Count == 0 || ranked[0].Score <= 0)
            return null;

        if (ranked[0].Score >= 100)
            return ranked[0].Player;

        if (ranked.Count == 1 || ranked[0].Score >= ranked[1].Score + 5)
            return ranked[0].Player;

        return null;
    }

    private static int ScorePlayerCandidate(string candidateName, string searchedName)
    {
        var normalizedCandidate = NormalizeNameForCompare(candidateName);
        var normalizedSearched = NormalizeNameForCompare(searchedName);

        if (string.IsNullOrWhiteSpace(normalizedCandidate) || string.IsNullOrWhiteSpace(normalizedSearched))
            return 0;

        if (normalizedCandidate == normalizedSearched)
            return 100;

        var candidateTokens = normalizedCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var searchedTokens = normalizedSearched.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var searchedTokenSet = searchedTokens.ToHashSet();

        int exactMatches = 0;
        int nearMatches = 0;

        foreach (var token in candidateTokens.Distinct())
        {
            if (searchedTokenSet.Contains(token))
            {
                exactMatches++;
            }
            else if (searchedTokens.Any(searched => AreCloseTokens(searched, token)))
            {
                nearMatches++;
            }
        }

        int score = (exactMatches * 10) + (nearMatches * 3);

        if (normalizedSearched.Contains(normalizedCandidate, StringComparison.Ordinal)
            || normalizedCandidate.Contains(normalizedSearched, StringComparison.Ordinal))
        {
            score += 8;
        }

        score -= Math.Abs(searchedTokens.Length - candidateTokens.Length);

        return score;
    }

    private static bool AreCloseTokens(string left, string right)
    {
        if (left == right)
            return true;

        if (Math.Abs(left.Length - right.Length) > 1)
            return false;

        return LevenshteinDistance(left, right) <= 1;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        if (left.Length == 0)
            return right.Length;

        if (right.Length == 0)
            return left.Length;

        var costs = new int[right.Length + 1];

        for (int j = 0; j <= right.Length; j++)
            costs[j] = j;

        for (int i = 1; i <= left.Length; i++)
        {
            int previousDiagonal = costs[0];
            costs[0] = i;

            for (int j = 1; j <= right.Length; j++)
            {
                int current = costs[j];
                int substitutionCost = left[i - 1] == right[j - 1] ? 0 : 1;

                costs[j] = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    previousDiagonal + substitutionCost);

                previousDiagonal = current;
            }
        }

        return costs[right.Length];
    }

    private static string NormalizeNameForCompare(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToUpperInvariant(c));
            }
            else
            {
                builder.Append(' ');
            }
        }

        return string.Join(
            " ",
            builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
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
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;

        var value = s.Trim();

        // Les fichiers de match stockent le chrono sous la forme mm:ss.
        if (TimeSpan.TryParseExact(
            value,
            new[] { @"m\:ss", @"mm\:ss", @"m\:s", @"mm\:s" },
            CultureInfo.InvariantCulture,
            out var matchClock))
        {
            return matchClock;
        }

        if (TimeSpan.TryParseExact(
            value,
            new[] { @"h\:mm\:ss", @"hh\:mm\:ss", "c", "g", "G" },
            CultureInfo.InvariantCulture,
            out var parsedExact))
        {
            return parsedExact;
        }

        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private string? ExtractMatchDay(string path)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            path ?? string.Empty,
            @"(?<!\w)J\d{1,2}(?!\w)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        var detectedDay = match.Value.ToUpperInvariant();
        return JoursDisponibles.Contains(detectedDay) ? detectedDay : null;
    }

    private static string? NormalizeMiTemps(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim().ToLowerInvariant();
        var digits = new string(t.Where(char.IsDigit).ToArray());
        if (digits == "1") return "MT1";
        if (digits == "2") return "MT2";
        if (t.Contains("mt1")) return "MT1";
        if (t.Contains("mt2")) return "MT2";
        return s.Trim();
    }

    private static bool IsNearHalfBoundary(TimeSpan? t)
    {
        if (t == null) return false;
        var minutes = t.Value.TotalMinutes;
        return minutes <= 1 || (minutes >= 29 && minutes <= 31);
    }

    private void LogHalfReset(int rowIndex, TimeSpan prevTime, TimeSpan currentTime, MatchFileDto dto)
    {
        var log =
        $"""
        {DateTime.Now}
        HALF-RESET détecté
        Row: {rowIndex}
        PrevTime: {prevTime}
        CurrentTime: {currentTime}
        Player: {dto.PlayerId}
        Team: {dto.TeamId}
        Score: {dto.TeamScore1}-{dto.TeamScore2}
        ----
        """;

        File.AppendAllText("integration_halftime.log", log);
    }

    private void LogSkip(
        string reason,
        int rowIndex,
        MatchFileDto dto,
        TimeSpan? time,
        string? miTemps,
        TimeSpan? prevTime,
        bool resetDetected,
        bool halftimeBoundary)
    {
        var log =
        $"""
        {DateTime.Now}
        SKIP: {reason}
        Row: {rowIndex}
        Player: {dto.PlayerId}
        Team: {dto.TeamId}
        Number: {dto.Number}
        Time: {dto.Time} (parsed: {time})
        Half: {miTemps}
        PrevTime: {prevTime}
        ResetDetected: {resetDetected}
        HalftimeBoundary: {halftimeBoundary}
        Score: {dto.TeamScore1}-{dto.TeamScore2}
        Event: {dto.EventId}
        ----
        """;

        File.AppendAllText("integration_skips.log", log);
    }

    private sealed class IdNameDto { public int Id { get; set; } }
    public class PlayerCreateDto
    {
        public string Name { get; set; }           
        public string Surname { get; set; }       
        public DateTime? Birthday { get; set; }
        public int PositionId { get; set; }
        public int TeamId { get; set; }
        public int NationalityId { get; set; }
        public int Number { get; set; }
    }
    public class TeamLight
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    private void LogSimple(string msg)
    {
        File.AppendAllText("integration_errors.log",
            $"{DateTime.Now} - {msg}\n");
    }


}
