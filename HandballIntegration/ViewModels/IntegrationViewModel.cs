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
    private readonly PlayersApiService _playersApiService;
    private readonly ApiSettings _settings;
    private static string Key(string s)
    => NormalizeImportedPlayerName(s).ToLowerInvariant();
    Dictionary<string, string> _playerNameMap = new();

    public List<string> JoursDisponibles { get; } =
    Enumerable.Range(1, 28)
              .Select(i => $"J{i}")
              .ToList();

    public List<string> SaisonsDisponibles { get; } =
        Enumerable.Range(2010, 21)
                  .Select(BuildSeasonLabel)
                  .ToList();

    public IntegrationViewModel()
    {

        _apiService = App.Services.GetRequiredService<ApiService>();
        _playersApiService = App.Services.GetRequiredService<PlayersApiService>();
        _http = App.Services.GetRequiredService<System.Net.Http.HttpClient>();

        var options = App.Services.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<ApiSettings>>();

        _settings = options.Value;
        IntegrateCommand = new AsyncRelayCommand<MatchToIntegrate>(IntegrateFileAsync);
        if (_settings == null)
        {
            Console.WriteLine("vide");

        }
        else if (string.IsNullOrWhiteSpace(_settings.ApiBaseUrl))
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
                            Day = detectedDay ?? JoursDisponibles.First(),
                            Season = GetCurrentSeasonLabel()
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

        _playerNameMap.Clear();

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

        teamNames = DetermineTeamOrderFromScoreColumns(rows, teamNames);

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
            Season = NormalizeSeason(file.MatchInfo.Season),
            Day = file.MatchInfo.Day
        };

        file.StatusMessage = "Preparation des evenements...";
        var pendingPlayerTeamUpdates = new Dictionary<int, PendingPlayerTeamUpdate>();
        var preparedEvents = await PrepareMatchEventsAsync(rows, teamsFromFile, pendingPlayerTeamUpdates);

        if (preparedEvents.Count == 0)
            throw new Exception("Aucun evenement valide a integrer.");

        file.StatusMessage = "Controle des doublons...";
        var existingMatch = await FindExistingIdenticalMatchAsync(newMatch, preparedEvents);
        if (existingMatch != null)
        {
            throw new Exception(
                $"Ce fichier correspond deja au match #{existingMatch.MatchId} du {existingMatch.Date:dd/MM/yyyy} ({existingMatch.Day}).");
        }

        if (pendingPlayerTeamUpdates.Count > 0)
        {
            file.StatusMessage = "Mise a jour des equipes joueuses...";
            await ApplyPendingPlayerTeamUpdatesAsync(pendingPlayerTeamUpdates.Values);
        }

        file.StatusMessage = "Creation du match...";
        var respMatch = await _http.PostAsJsonAsync($"{_settings.ApiBaseUrl}api/Matches", newMatch);
        respMatch.EnsureSuccessStatusCode();

        var createdMatch = await respMatch.Content.ReadFromJsonAsync<Match>()
            ?? throw new Exception("La creation du match a reussi mais la reponse est vide.");

        file.StatusMessage = "Importation evenements...";
        foreach (var preparedEvent in preparedEvents)
        {
            preparedEvent.MatchEvent.MatchId = createdMatch.Id;

            var resp = await _http.PostAsJsonAsync($"{_settings.ApiBaseUrl}api/MatchEvents", preparedEvent.MatchEvent);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();

                var sentJson = System.Text.Json.JsonSerializer.Serialize(
                    preparedEvent.MatchEvent,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                var log =
            $"""
                    ==============================
                    {DateTime.Now}
                    
                    ROUTE : POST api/MatchEvents
                    STATUS : {(int)resp.StatusCode} {resp.ReasonPhrase}
                    
                    ----- OBJET ENVOYE -----
                    {sentJson}
                    
                    ----- REPONSE API -----
                    {body}
                    
                    ==============================
                    
                    """;

                File.AppendAllText("integration_errors.log", log);

                continue;
            }

            resp.EnsureSuccessStatusCode();
        }
    }
       

    private async Task<List<PreparedMatchEvent>> PrepareMatchEventsAsync(
        List<MatchFileDto> rows,
        List<TeamLight> teamsFromFile,
        Dictionary<int, PendingPlayerTeamUpdate> pendingPlayerTeamUpdates)
    {
        var preparedEvents = new List<PreparedMatchEvent>();
        int currentHalf = 1;
        TimeSpan? prevTime = null;

        for (int i = 0; i < rows.Count; i++)
        {
            var dto = rows[i];
            int rowIndex = i + 2;
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

            if (int.TryParse(dto.Number, out var dtoNumber) && dtoNumber >= 100)
            {
                LogSkip("Number>=100", rowIndex, dto, parsedTime, miTemps, prevTimeForLog, resetDetected, isBoundary);
                continue;
            }

            int? teamId = ResolveTeamIdFromFile(dto.TeamId, teamsFromFile);
            if (teamId == null)
            {
                teamId = await ApiResolveTeamId(dto.TeamId);
            }

            var resolvedPlayer = await ResolvePlayerAsync(dto, teamId, teamsFromFile);
            int? eventId = await ApiResolveIdEvent($"{_settings.ApiBaseUrl}api/Event/", dto.EventId);
            int? attackId = await ApiResolveIdAttack(dto.AttackId);
            int? defenseId = await ApiResolveIdDefense(dto.DefenseId);

            if (teamId == null)
            {
                LogSkip("Team inconnue", rowIndex, dto, parsedTime, miTemps, prevTimeForLog, resetDetected, isBoundary);
                continue;
            }

            if (resolvedPlayer == null)
            {
                LogSkip("Player inconnu", rowIndex, dto, parsedTime, miTemps, prevTimeForLog, resetDetected, isBoundary);
                continue;
            }

            if (eventId == null)
            {
                LogSimple($"Event inconnu '{dto.EventId}' -> force a ID 37");
                eventId = 37;
            }

            RegisterPendingPlayerTeamUpdate(
                pendingPlayerTeamUpdates,
                resolvedPlayer,
                teamId.Value,
                dto.TeamId);

            preparedEvents.Add(new PreparedMatchEvent
            {
                SourceRowIndex = rowIndex,
                MatchEvent = new MatchEvent
                {
                    TeamId = teamId.Value,
                    PlayerId = resolvedPlayer.Id,
                    EventId = eventId.Value,
                    AttackId = attackId,
                    DefenseId = defenseId,
                    Time = parsedTime,
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
                }
            });
        }

        return preparedEvents;
    }

    private async Task<PlayerListItemDto?> ResolvePlayerAsync(
        MatchFileDto dto,
        int? teamId,
        List<TeamLight> teamsFromFile)
    {
        dto.PlayerId = NormalizeImportedPlayerName(dto.PlayerId);
        string originalName = dto.PlayerId;
        var key = Key(originalName);

        if (_playerNameMap.TryGetValue(key, out var mappedName))
        {
            dto.PlayerId = mappedName;
        }

        var resolvedPlayer = await ApiResolvePlayerAsync(dto.PlayerId);
        if (resolvedPlayer != null)
        {
            _playerNameMap[key] = resolvedPlayer.FullName;
            return resolvedPlayer;
        }

        if (string.IsNullOrWhiteSpace(dto.PlayerId))
        {
            return null;
        }

        var parts = dto.PlayerId.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string prenom = parts.Length > 0 ? parts[0] : string.Empty;
        string nom = parts.Length > 1 ? parts[^1] : string.Empty;

        var candidates = await ApiSearchPlayersApprox(dto.PlayerId);
        if (!candidates.Any())
        {
            if (!string.IsNullOrWhiteSpace(prenom))
                candidates.AddRange(await ApiSearchPlayersApprox(prenom));

            if (!string.IsNullOrWhiteSpace(nom))
                candidates.AddRange(await ApiSearchPlayersApprox(nom));
        }

        candidates = candidates
            .GroupBy(player => player.Id)
            .Select(group => group.First())
            .ToList();

        PlayerListItemDto? selected = null;
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

        if (selected != null)
        {
            _playerNameMap[key] = selected.FullName;
            return selected;
        }

        var createdPlayer = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var win = new AddPlayerWindows(prenom, nom, teamsFromFile);
            win.Owner = System.Windows.Application.Current.MainWindow;
            return win.ShowDialog() == true ? win.CreatedPlayer : null;
        });

        if (createdPlayer == null)
        {
            return null;
        }

        var createdPlayerDto = ToPlayerListItemDto(createdPlayer);
        if (teamId.HasValue)
        {
            createdPlayerDto.TeamId = teamId.Value;
        }

        _playerNameMap[key] = createdPlayerDto.FullName;
        return createdPlayerDto;
    }

    private async Task<MatchListItemDto?> FindExistingIdenticalMatchAsync(
        Match expectedMatch,
        IReadOnlyList<PreparedMatchEvent> preparedEvents)
    {
        if (expectedMatch.Date == null)
        {
            return null;
        }

        string dateValue = expectedMatch.Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var matches = await _http.GetFromJsonAsync<List<MatchListItemDto>>(
            $"{_settings.ApiBaseUrl}api/Matches?competitionId={expectedMatch.CompetitionId}&from={dateValue}&to={dateValue}&pageSize=500")
            ?? new List<MatchListItemDto>();

        var candidates = matches.Where(match =>
                match.CompetitionId == expectedMatch.CompetitionId
                && match.Date?.Date == expectedMatch.Date.Value.Date
                && string.Equals(NormalizeText(match.Season), NormalizeText(expectedMatch.Season), StringComparison.Ordinal)
                && string.Equals(NormalizeText(match.Day), NormalizeText(expectedMatch.Day), StringComparison.Ordinal)
                && match.Team1Id == expectedMatch.Team1Id
                && match.Team2Id == expectedMatch.Team2Id
                && match.Team1Score == expectedMatch.Team1Score
                && match.Team2Score == expectedMatch.Team2Score)
            .ToList();

        foreach (var candidate in candidates)
        {
            var existingEvents = await GetExistingMatchEventsAsync(candidate.MatchId);
            if (AreMatchEventsIdentical(preparedEvents, existingEvents))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<List<MatchEvent>> GetExistingMatchEventsAsync(int matchId)
    {
        return await _http.GetFromJsonAsync<List<MatchEvent>>(
            $"{_settings.ApiBaseUrl}api/MatchEvents?matchId={matchId}")
            ?? new List<MatchEvent>();
    }

    private async Task ApplyPendingPlayerTeamUpdatesAsync(IEnumerable<PendingPlayerTeamUpdate> updates)
    {
        foreach (var update in updates)
        {
            bool success = await _playersApiService.UpdatePlayerTeamAsync(update.PlayerId, update.TeamId);
            if (!success)
            {
                throw new Exception(
                    $"Impossible de mettre a jour l'equipe de {update.PlayerName} vers {update.TeamName}.");
            }

            LogSimple($"Equipe synchronisee pour '{update.PlayerName}' -> '{update.TeamName}'");
        }
    }

    // ---------------- HELPERS -------------------
    private async Task<int?> ApiResolveTeamId(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var resp = await _http.GetAsync($"{_settings.ApiBaseUrl}teams/byname/{Uri.EscapeDataString(name)}");
        
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
            $"{_settings.ApiBaseUrl}api/Attacks/byname/",
            $"{_settings.ApiBaseUrl}api/Attacks/"
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
            $"{_settings.ApiBaseUrl}api/Defenses/byname/",
            $"{_settings.ApiBaseUrl}api/Defenses/"
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
    private async Task<PlayerListItemDto?> ApiResolvePlayerAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var resp = await _http.GetAsync($"{_settings.ApiBaseUrl}api/Players/byfullname/{Uri.EscapeDataString(name)}");

        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<PlayerListItemDto>();
    }
    private async Task<List<PlayerListItemDto>> ApiSearchPlayersApprox(string name)
    {
        var resp = await _http.GetAsync(
            $"{_settings.ApiBaseUrl}api/Players/search/{Uri.EscapeDataString(name)}");

        if (!resp.IsSuccessStatusCode)
            return new List<PlayerListItemDto>();

        return await resp.Content.ReadFromJsonAsync<List<PlayerListItemDto>>()
               ?? new List<PlayerListItemDto>();
    }
    private Task<PlayerListItemDto?> ShowPlayerSelectionAsync(List<PlayerListItemDto> candidates, string searchedName)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = new FoundPlayersWindows(candidates, searchedName);
            window.Owner = System.Windows.Application.Current.MainWindow;

            bool? result = window.ShowDialog();
            return result == true ? window.SelectedPlayer : null;
        }).Task;
    }

    private PlayerListItemDto? TryAutoSelectPlayerCandidate(List<PlayerListItemDto> candidates, string searchedName)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

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

        if (ranked.Count == 1)
            return ranked[0].Score >= 18 ? ranked[0].Player : null;

        if (ranked[0].Score >= 18 && ranked[0].Score >= ranked[1].Score + 5)
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

    private static string NormalizeImportedPlayerName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        int start = 0;
        int end = trimmed.Length - 1;

        while (start <= end && !char.IsLetterOrDigit(trimmed[start]))
        {
            start++;
        }

        while (end >= start && !char.IsLetterOrDigit(trimmed[end]))
        {
            end--;
        }

        if (start > end)
            return string.Empty;

        return string.Join(
            " ",
            trimmed[start..(end + 1)]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool AreMatchEventsIdentical(
        IReadOnlyList<PreparedMatchEvent> currentEvents,
        IReadOnlyList<MatchEvent> existingEvents)
    {
        if (currentEvents.Count != existingEvents.Count)
            return false;

        var currentGroups = currentEvents
            .GroupBy(item => BuildMatchEventFingerprint(item.MatchEvent))
            .ToDictionary(group => group.Key, group => group.Count());

        var existingGroups = existingEvents
            .GroupBy(BuildMatchEventFingerprint)
            .ToDictionary(group => group.Key, group => group.Count());

        if (currentGroups.Count != existingGroups.Count)
            return false;

        foreach (var currentGroup in currentGroups)
        {
            if (!existingGroups.TryGetValue(currentGroup.Key, out var existingCount)
                || existingCount != currentGroup.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildMatchEventFingerprint(MatchEvent matchEvent)
    {
        string timeValue = matchEvent.Time.HasValue
            ? ((int)matchEvent.Time.Value.TotalSeconds).ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        return string.Join("|", new[]
        {
            matchEvent.TeamId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            matchEvent.PlayerId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            matchEvent.EventId.ToString(CultureInfo.InvariantCulture),
            matchEvent.AttackId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            matchEvent.DefenseId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            timeValue,
            NormalizeText(matchEvent.MiTemps),
            matchEvent.TeamScore1?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            matchEvent.TeamScore2?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            NormalizeText(matchEvent.Action),
            NormalizeText(matchEvent.ShootZone),
            NormalizeText(matchEvent.Shade),
            NormalizeText(matchEvent.ShootShade),
            NormalizeText(matchEvent.ArmSide),
            NormalizeText(matchEvent.Jump),
            NormalizeText(matchEvent.Trigger),
            NormalizeText(matchEvent.PlayerNumber1),
            NormalizeText(matchEvent.PlayerNumber2)
        });
    }

    private static string NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();

    private static string BuildSeasonLabel(int startYear)
        => $"{startYear}-{startYear + 1}";

    private string GetCurrentSeasonLabel()
    {
        int currentYear = DateTime.Today.Year;
        int startYear = DateTime.Today.Month >= 7
            ? currentYear
            : currentYear - 1;

        startYear = Math.Clamp(startYear, 2010, 2030);
        return BuildSeasonLabel(startYear);
    }

    private static string? NormalizeSeason(string? season)
    {
        if (string.IsNullOrWhiteSpace(season))
            return null;

        return season.Trim();
    }

    private static int? ResolveTeamIdFromFile(string? teamName, IEnumerable<TeamLight> teamsFromFile)
    {
        if (string.IsNullOrWhiteSpace(teamName))
            return null;

        return teamsFromFile
            .FirstOrDefault(team => string.Equals(
                team.Name?.Trim(),
                teamName.Trim(),
                StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }

    private static void RegisterPendingPlayerTeamUpdate(
        Dictionary<int, PendingPlayerTeamUpdate> pendingPlayerTeamUpdates,
        PlayerListItemDto resolvedPlayer,
        int currentTeamId,
        string? currentTeamName)
    {
        if (resolvedPlayer.Id <= 0)
            return;

        if (resolvedPlayer.TeamId == currentTeamId)
            return;

        pendingPlayerTeamUpdates[resolvedPlayer.Id] = new PendingPlayerTeamUpdate
        {
            PlayerId = resolvedPlayer.Id,
            PlayerName = resolvedPlayer.FullName,
            TeamId = currentTeamId,
            TeamName = string.IsNullOrWhiteSpace(currentTeamName)
                ? currentTeamId.ToString(CultureInfo.InvariantCulture)
                : currentTeamName.Trim()
        };
    }

    private static PlayerListItemDto ToPlayerListItemDto(Player player)
    {
        return new PlayerListItemDto
        {
            PlayerId = player.Id,
            FullName = player.FullName,
            TeamId = player.TeamId,
            PositionId = player.PositionId,
            Age = player.Age,
            Number = player.Number,
            Birthday = player.Birthday,
            IsActive = player.IsActive
        };
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

    private static List<string> DetermineTeamOrderFromScoreColumns(
        IReadOnlyList<MatchFileDto> rows,
        IReadOnlyList<string> detectedTeams)
    {
        var teams = detectedTeams
            .Where(team => !string.IsNullOrWhiteSpace(team))
            .Select(team => team.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        if (teams.Count != 2)
            return teams;

        var scoreColumnToTeam = new Dictionary<int, string>();
        int? previousScoreA = 0;
        int? previousScoreB = 0;

        foreach (var row in rows)
        {
            int currentScoreA = TryParseInt(row.TeamScore1) ?? previousScoreA ?? 0;
            int currentScoreB = TryParseInt(row.TeamScore2) ?? previousScoreB ?? 0;
            int deltaA = currentScoreA - (previousScoreA ?? 0);
            int deltaB = currentScoreB - (previousScoreB ?? 0);
            string currentTeam = (row.TeamId ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(currentTeam) && IsScoringEvent(row.EventId))
            {
                if (deltaA > 0 && deltaB <= 0)
                {
                    scoreColumnToTeam[1] = currentTeam;
                }
                else if (deltaB > 0 && deltaA <= 0)
                {
                    scoreColumnToTeam[2] = currentTeam;
                }
            }

            previousScoreA = currentScoreA;
            previousScoreB = currentScoreB;

            if (scoreColumnToTeam.Count == 2)
                break;
        }

        if (scoreColumnToTeam.TryGetValue(1, out var teamForScoreA)
            && scoreColumnToTeam.TryGetValue(2, out var teamForScoreB))
        {
            return new List<string> { teamForScoreA, teamForScoreB };
        }

        if (scoreColumnToTeam.TryGetValue(1, out teamForScoreA))
        {
            var otherTeam = teams.FirstOrDefault(team => !string.Equals(team, teamForScoreA, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(otherTeam))
                return new List<string> { teamForScoreA, otherTeam };
        }

        if (scoreColumnToTeam.TryGetValue(2, out teamForScoreB))
        {
            var otherTeam = teams.FirstOrDefault(team => !string.Equals(team, teamForScoreB, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(otherTeam))
                return new List<string> { otherTeam, teamForScoreB };
        }

        return teams;
    }

    private static bool IsScoringEvent(string? eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return false;

        var normalized = NormalizeText(eventName);
        return normalized.StartsWith("BUT", StringComparison.Ordinal)
            && !normalized.Contains("GARDIEN PREND", StringComparison.Ordinal);
    }

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
        public int? Age { get; set; }
        public int? PositionId { get; set; }
        public int? TeamId { get; set; }
        public int? NationalityId { get; set; }
        public int? Number { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class TeamLight
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class PreparedMatchEvent
    {
        public int SourceRowIndex { get; set; }
        public MatchEvent MatchEvent { get; set; } = new();
    }

    private sealed class PendingPlayerTeamUpdate
    {
        public int PlayerId { get; set; }
        public int TeamId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
    }
    private void LogSimple(string msg)
    {
        File.AppendAllText("integration_errors.log",
            $"{DateTime.Now} - {msg}\n");
    }


}
