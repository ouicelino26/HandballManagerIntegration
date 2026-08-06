using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandballIntegration.Data;
using HandballIntegration.Services;
using HandballIntegration.Views;
using HandballManagerCore.DTO;
using HandballManagerCore.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

namespace HandballIntegration.ViewModels
{
    public partial class TimeIntegrationViewModel : ObservableObject
    {
        private readonly HttpClient _http;
        private readonly ApiService _apiService;
        private readonly ApiSettings _settings;
        private readonly TimePlayersSheetImportService _sheetImportService = new();
        private readonly Dictionary<string, string> _playerNameMap = new();

        public List<string> JoursDisponibles { get; } =
            Enumerable.Range(1, 28)
                .Select(index => $"J{index}")
                .ToList();

        public List<string> SaisonsDisponibles { get; } =
            Enumerable.Range(2010, 21)
                .Select(BuildSeasonLabel)
                .ToList();

        [ObservableProperty]
        private string selectedFolder = string.Empty;

        public ObservableCollection<TimePlayersFileToIntegrate> Files { get; } = new();

        public IRelayCommand<TimePlayersFileToIntegrate> IntegrateCommand { get; }

        public TimeIntegrationViewModel()
        {
            _apiService = App.Services.GetRequiredService<ApiService>();
            _http = App.Services.GetRequiredService<HttpClient>();
            _settings = App.Services.GetRequiredService<IOptions<ApiSettings>>().Value;
            IntegrateCommand = new AsyncRelayCommand<TimePlayersFileToIntegrate>(IntegrateFileAsync);
        }

        public void LoadFiles(string folderPath)
        {
            SelectedFolder = folderPath;
            Files.Clear();

            foreach (var filePath in Directory.GetFiles(folderPath, "*.xlsx", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(filePath);
                if (!fileName.StartsWith("Table historique des actions du match", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Files.Add(new TimePlayersFileToIntegrate
                {
                    FileName = fileName,
                    FullPath = filePath,
                    TeamsLabel = BuildTeamsLabel(filePath),
                    MatchInfo = new MatchDto
                    {
                        Day = ExtractMatchDay(filePath) ?? JoursDisponibles.First(),
                        Season = GetCurrentSeasonLabel()
                    }
                });
            }
        }

        public async Task IntegrateFileAsync(TimePlayersFileToIntegrate? file)
        {
            if (file == null)
            {
                return;
            }

            file.IsBusy = true;
            file.Status = IntegrationStatus.Converting;
            file.StatusMessage = "Lecture du fichier...";

            try
            {
                var summary = await IntegrateInternalAsync(file);
                file.Status = IntegrationStatus.Success;
                file.StatusMessage = summary;
            }
            catch (Exception ex)
            {
                file.Status = IntegrationStatus.Error;
                file.StatusMessage = "Erreur : " + ex.Message;
            }
            finally
            {
                file.IsBusy = false;
            }
        }

        private async Task<string> IntegrateInternalAsync(TimePlayersFileToIntegrate file)
        {
            if (!await _apiService.PrepareAuthorizedClientAsync(_http))
            {
                throw new Exception("Session administrateur requise.");
            }

            _playerNameMap.Clear();

            file.Status = IntegrationStatus.Integrating;
            file.StatusMessage = "Recherche du match...";

            var folderTeamCodes = ExtractFolderTeamCodes(file.FullPath);
            var teams = await ResolveTeamsAsync(file.FullPath, folderTeamCodes);
            if (teams.Count != 2)
            {
                throw new Exception("Impossible d'identifier les deux equipes du fichier.");
            }

            file.TeamsLabel = string.Join(" / ", teams.Select(team => team.DisplayLabel));

            var existingMatch = await FindExistingMatchAsync(file.MatchInfo.Season, file.MatchInfo.Day, teams);
            if (existingMatch == null)
            {
                throw new Exception("Aucun match existant ne correspond a cette saison, cette journee et ces equipes.");
            }

            var existingTimeRows = await _http.GetFromJsonAsync<List<TimePlayers>>(
                $"{_settings.ApiBaseUrl}api/TimePlayers?matchId={existingMatch.MatchId}")
                ?? new List<TimePlayers>();

            if (existingTimeRows.Any(item => item.MatchId == existingMatch.MatchId))
            {
                throw new Exception($"Des temps de jeu sont deja integres pour le match #{existingMatch.MatchId}.");
            }

            file.StatusMessage = "Lecture de Feuil1...";
            var importedRows = _sheetImportService.ReadTimeRows(file.FullPath);
            if (importedRows.Count == 0)
            {
                throw new Exception("Aucune ligne de temps exploitable n'a ete trouvee dans Feuil1.");
            }

            var teamsForAddWindow = teams
                .Select(team => new IntegrationViewModel.TeamLight
                {
                    Id = team.Team.TeamId,
                    Name = team.Team.TeamName ?? team.Team.TeamCode ?? "Equipe"
                })
                .ToList();

            var teamMappings = BuildSectionTeamMappings(importedRows, teams);

            int importedCount = 0;
            int skippedCount = 0;

            foreach (var row in importedRows)
            {
                file.StatusMessage = $"Resolution joueuse {importedCount + skippedCount + 1}/{importedRows.Count}...";

                int? expectedTeamId = null;
                var sectionKey = NormalizeTeamKey(row.TeamLabel);
                if (teamMappings.TryGetValue(sectionKey, out var mappedTeam))
                {
                    expectedTeamId = mappedTeam.Team.TeamId;
                }

                var player = await ResolvePlayerAsync(row.PlayerName, expectedTeamId, teamsForAddWindow);
                if (player == null)
                {
                    skippedCount++;
                    LogSimple($"Time import skipped row {row.RowNumber}: player '{row.PlayerName}' introuvable.");
                    continue;
                }

                var effectiveTeamLabel = mappedTeam?.DisplayLabel;
                if (string.IsNullOrWhiteSpace(effectiveTeamLabel))
                {
                    effectiveTeamLabel = string.IsNullOrWhiteSpace(row.TeamLabel) ? "Equipe" : row.TeamLabel;
                }

                var payload = new TimePlayers
                {
                    MatchId = existingMatch.MatchId,
                    TeamLabel = effectiveTeamLabel,
                    PlayerName = player.FullName,
                    PlayingTime = row.MatchTime,
                    PlayerId = player.Id,
                    SourceFile = Path.GetFileName(file.FullPath),
                    SourceSheet = "Feuil1",
                    SourceRow = row.RowNumber,
                    Version = 1,
                    ConcurrencyToken = Guid.NewGuid(),
                    UpdatedAtUtc = DateTime.UtcNow,
                    UpdatedBy = "integration-client",
                    IsDeleted = false
                };

                var response = await _http.PostAsJsonAsync($"{_settings.ApiBaseUrl}api/TimePlayers", payload);
                if (!response.IsSuccessStatusCode)
                {
                    skippedCount++;
                    var body = await response.Content.ReadAsStringAsync();
                    LogSimple($"Time import failed row {row.RowNumber} / player '{player.FullName}': {(int)response.StatusCode} {body}");
                    continue;
                }

                importedCount++;
            }

            if (importedCount == 0)
            {
                throw new Exception("Aucun temps de jeu n'a pu etre integre.");
            }

            return skippedCount == 0
                ? $"{importedCount} temps de jeu integres"
                : $"{importedCount} temps integres, {skippedCount} ignores";
        }

        private async Task<List<ResolvedMatchTeam>> ResolveTeamsAsync(string workbookPath, IReadOnlyList<string> folderTeamCodes)
        {
            var teams = new List<ResolvedMatchTeam>();

            foreach (var code in folderTeamCodes)
            {
                var team = await ApiResolveTeamByCodeAsync(code);
                if (team == null || teams.Any(item => item.Team.TeamId == team.TeamId))
                {
                    continue;
                }

                teams.Add(new ResolvedMatchTeam
                {
                    RequestedCode = code,
                    Team = team
                });
            }

            var fallbackNames = _sheetImportService.ReadMatchTeamNames(workbookPath);
            foreach (var name in fallbackNames)
            {
                var team = await ApiResolveTeamByNameAsync(name);
                if (team == null || teams.Any(item => item.Team.TeamId == team.TeamId))
                {
                    continue;
                }

                teams.Add(new ResolvedMatchTeam
                {
                    RequestedCode = folderTeamCodes.FirstOrDefault(code => !teams.Any(item => string.Equals(item.RequestedCode, code, StringComparison.OrdinalIgnoreCase))),
                    Team = team
                });
            }

            return teams.Take(2).ToList();
        }

        private async Task<MatchListItemDto?> FindExistingMatchAsync(string? season, string? day, IReadOnlyList<ResolvedMatchTeam> teams)
        {
            var normalizedSeason = string.IsNullOrWhiteSpace(season) ? null : season.Trim();
            var normalizedDay = string.IsNullOrWhiteSpace(day) ? null : day.Trim().ToUpperInvariant();

            var requestUrl = $"{_settings.ApiBaseUrl}api/Matches?competitionId=1&pageSize=500";
            if (!string.IsNullOrWhiteSpace(normalizedSeason))
            {
                requestUrl += $"&season={Uri.EscapeDataString(normalizedSeason)}";
            }

            if (!string.IsNullOrWhiteSpace(normalizedDay))
            {
                requestUrl += $"&day={Uri.EscapeDataString(normalizedDay)}";
            }

            var matches = await _http.GetFromJsonAsync<List<MatchListItemDto>>(requestUrl)
                ?? new List<MatchListItemDto>();

            var teamIds = teams.Select(team => team.Team.TeamId).OrderBy(id => id).ToArray();

            var candidates = matches.Where(match =>
                string.Equals((match.Season ?? string.Empty).Trim(), normalizedSeason ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && string.Equals((match.Day ?? string.Empty).Trim(), normalizedDay ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && match.Team1Id.HasValue
                && match.Team2Id.HasValue
                && new[] { match.Team1Id.Value, match.Team2Id.Value }.OrderBy(id => id).SequenceEqual(teamIds))
                .ToList();

            return candidates.Count switch
            {
                0 => null,
                1 => candidates[0],
                _ => throw new Exception("Plusieurs matchs correspondent au fichier. Verifie la saison et la journee.")
            };
        }

        private Dictionary<string, ResolvedMatchTeam> BuildSectionTeamMappings(
            IReadOnlyList<TimePlayerImportRow> rows,
            IReadOnlyList<ResolvedMatchTeam> teams)
        {
            var mappings = new Dictionary<string, ResolvedMatchTeam>(StringComparer.OrdinalIgnoreCase);
            var remainingTeams = teams.ToList();

            var labels = rows
                .Select(row => row.TeamLabel)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var label in labels.ToList())
            {
                var normalizedLabel = NormalizeTeamKey(label);
                var candidates = remainingTeams
                    .Where(team => TeamLabelMatches(label, team))
                    .ToList();

                if (candidates.Count == 1)
                {
                    mappings[normalizedLabel] = candidates[0];
                    remainingTeams.Remove(candidates[0]);
                }
            }

            foreach (var label in labels)
            {
                var normalizedLabel = NormalizeTeamKey(label);
                if (mappings.ContainsKey(normalizedLabel))
                {
                    continue;
                }

                if (remainingTeams.Count == 1)
                {
                    mappings[normalizedLabel] = remainingTeams[0];
                    remainingTeams.Clear();
                }
            }

            return mappings;
        }

        private async Task<PlayerListItemDto?> ResolvePlayerAsync(
            string rawName,
            int? expectedTeamId,
            List<IntegrationViewModel.TeamLight> teamsForAddWindow)
        {
            var normalizedName = NormalizeImportedPlayerName(rawName);
            var key = normalizedName.ToLowerInvariant();

            if (_playerNameMap.TryGetValue(key, out var mappedName))
            {
                normalizedName = mappedName;
            }

            var resolved = await ApiResolvePlayerAsync(normalizedName);
            if (resolved != null)
            {
                _playerNameMap[key] = resolved.FullName;
                return resolved;
            }

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return null;
            }

            var parts = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = parts.Length > 0 ? parts[0] : string.Empty;
            var lastName = parts.Length > 1 ? parts[^1] : string.Empty;

            var candidates = await ApiSearchPlayersApprox(normalizedName);
            if (!candidates.Any())
            {
                if (!string.IsNullOrWhiteSpace(firstName))
                {
                    candidates.AddRange(await ApiSearchPlayersApprox(firstName));
                }

                if (!string.IsNullOrWhiteSpace(lastName))
                {
                    candidates.AddRange(await ApiSearchPlayersApprox(lastName));
                }
            }

            candidates = candidates
                .GroupBy(candidate => candidate.Id)
                .Select(group => group.First())
                .ToList();

            PlayerListItemDto? selected = null;
            if (candidates.Any())
            {
                selected = TryAutoSelectPlayerCandidate(candidates, normalizedName, expectedTeamId);
                if (selected == null)
                {
                    selected = await ShowPlayerSelectionAsync(candidates, normalizedName);
                }
            }

            if (selected != null)
            {
                _playerNameMap[key] = selected.FullName;
                return selected;
            }

            var sortedTeams = expectedTeamId.HasValue
                ? teamsForAddWindow.OrderByDescending(team => team.Id == expectedTeamId.Value).ToList()
                : teamsForAddWindow;

            var createdPlayer = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = new AddPlayerWindows(firstName, lastName, sortedTeams);
                window.Owner = System.Windows.Application.Current.MainWindow;
                return window.ShowDialog() == true ? window.CreatedPlayer : null;
            });

            if (createdPlayer == null)
            {
                return null;
            }

            var dto = new PlayerListItemDto
            {
                PlayerId = createdPlayer.Id,
                FullName = createdPlayer.FullName,
                TeamId = createdPlayer.TeamId,
                TeamName = sortedTeams.FirstOrDefault(team => team.Id == createdPlayer.TeamId)?.Name,
                Age = createdPlayer.Age,
                Number = createdPlayer.Number,
                Birthday = createdPlayer.Birthday,
                IsActive = createdPlayer.IsActive
            };

            _playerNameMap[key] = dto.FullName;
            return dto;
        }

        private async Task<PlayerListItemDto?> ApiResolvePlayerAsync(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var response = await _http.GetAsync($"{_settings.ApiBaseUrl}api/Players/byfullname/{Uri.EscapeDataString(name)}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PlayerListItemDto>();
        }

        private async Task<List<PlayerListItemDto>> ApiSearchPlayersApprox(string name)
        {
            var response = await _http.GetAsync($"{_settings.ApiBaseUrl}api/Players/search/{Uri.EscapeDataString(name)}");
            if (!response.IsSuccessStatusCode)
            {
                return new List<PlayerListItemDto>();
            }

            return await response.Content.ReadFromJsonAsync<List<PlayerListItemDto>>() ?? new List<PlayerListItemDto>();
        }

        private async Task<TeamDto?> ApiResolveTeamByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var response = await _http.GetAsync($"{_settings.ApiBaseUrl}teams/by-code/{Uri.EscapeDataString(code)}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TeamDto>();
        }

        private async Task<TeamDto?> ApiResolveTeamByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var response = await _http.GetAsync($"{_settings.ApiBaseUrl}teams/byname/{Uri.EscapeDataString(name)}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TeamDto>();
        }

        private Task<PlayerListItemDto?> ShowPlayerSelectionAsync(List<PlayerListItemDto> candidates, string searchedName)
        {
            return System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = new FoundPlayersWindows(candidates, searchedName);
                window.Owner = System.Windows.Application.Current.MainWindow;
                return window.ShowDialog() == true ? window.SelectedPlayer : null;
            }).Task;
        }

        private PlayerListItemDto? TryAutoSelectPlayerCandidate(
            List<PlayerListItemDto> candidates,
            string searchedName,
            int? expectedTeamId)
        {
            var ranked = candidates
                .Select(candidate => new
                {
                    Player = candidate,
                    Score = ScorePlayerCandidate(candidate.FullName, searchedName)
                        + (expectedTeamId.HasValue && candidate.TeamId == expectedTeamId.Value ? 12 : 0)
                })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => NormalizeNameForCompare(item.Player.FullName).Length)
                .ToList();

            if (ranked.Count == 0 || ranked[0].Score <= 0)
            {
                return null;
            }

            if (ranked[0].Score >= 100)
            {
                return ranked[0].Player;
            }

            if (ranked.Count == 1)
            {
                return ranked[0].Score >= 18 ? ranked[0].Player : null;
            }

            return ranked[0].Score >= 18 && ranked[0].Score >= ranked[1].Score + 5
                ? ranked[0].Player
                : null;
        }

        private static int ScorePlayerCandidate(string candidateName, string searchedName)
        {
            var normalizedCandidate = NormalizeNameForCompare(candidateName);
            var normalizedSearched = NormalizeNameForCompare(searchedName);

            if (string.IsNullOrWhiteSpace(normalizedCandidate) || string.IsNullOrWhiteSpace(normalizedSearched))
            {
                return 0;
            }

            if (normalizedCandidate == normalizedSearched)
            {
                return 100;
            }

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
            {
                return true;
            }

            if (Math.Abs(left.Length - right.Length) > 1)
            {
                return false;
            }

            return LevenshteinDistance(left, right) <= 1;
        }

        private static int LevenshteinDistance(string left, string right)
        {
            if (left.Length == 0)
            {
                return right.Length;
            }

            if (right.Length == 0)
            {
                return left.Length;
            }

            var costs = new int[right.Length + 1];
            for (int index = 0; index <= right.Length; index++)
            {
                costs[index] = index;
            }

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
            {
                return string.Empty;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                builder.Append(char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : ' ');
            }

            return string.Join(" ", builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string NormalizeImportedPlayerName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

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
            {
                return string.Empty;
            }

            return string.Join(" ", trimmed[start..(end + 1)].Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string NormalizeTeamKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        }

        private static bool TeamLabelMatches(string label, ResolvedMatchTeam team)
        {
            var normalizedLabel = NormalizeTeamKey(label);
            if (string.IsNullOrWhiteSpace(normalizedLabel))
            {
                return false;
            }

            var candidates = new[]
            {
                NormalizeTeamKey(team.RequestedCode),
                NormalizeTeamKey(team.Team.TeamCode),
                NormalizeTeamKey(team.Team.TeamName)
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

            if (candidates.Any(candidate => candidate == normalizedLabel))
            {
                return true;
            }

            return candidates.Any(candidate =>
                candidate.Contains(normalizedLabel, StringComparison.OrdinalIgnoreCase)
                || normalizedLabel.Contains(candidate, StringComparison.OrdinalIgnoreCase)
                || CommonPrefixLength(candidate, normalizedLabel) >= 4);
        }

        private static int CommonPrefixLength(string left, string right)
        {
            int max = Math.Min(left.Length, right.Length);
            int index = 0;

            while (index < max && left[index] == right[index])
            {
                index++;
            }

            return index;
        }

        private static string BuildTeamsLabel(string path)
        {
            var codes = ExtractFolderTeamCodes(path);
            return codes.Count == 2 ? $"{codes[0]} / {codes[1]}" : "Equipes a confirmer";
        }

        private static List<string> ExtractFolderTeamCodes(string path)
        {
            var folderName = new DirectoryInfo(Path.GetDirectoryName(path) ?? string.Empty).Name;
            var match = System.Text.RegularExpressions.Regex.Match(
                folderName,
                @"J\d{1,2}\s+(.+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return new List<string>();
            }

            return match.Groups[1].Value
                .Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim().ToUpperInvariant())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Take(2)
                .ToList();
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

            var day = match.Value.ToUpperInvariant();
            return JoursDisponibles.Contains(day) ? day : null;
        }

        private static string BuildSeasonLabel(int startYear)
            => $"{startYear}-{startYear + 1}";

        private string GetCurrentSeasonLabel()
        {
            int currentYear = DateTime.Today.Year;
            int startYear = DateTime.Today.Month >= 7 ? currentYear : currentYear - 1;
            startYear = Math.Clamp(startYear, 2010, 2030);
            return BuildSeasonLabel(startYear);
        }

        private void LogSimple(string message)
        {
            File.AppendAllText("integration_time_errors.log", $"{DateTime.Now} - {message}{Environment.NewLine}");
        }

        private sealed class ResolvedMatchTeam
        {
            public string? RequestedCode { get; set; }
            public TeamDto Team { get; set; } = new();
            public string DisplayLabel => !string.IsNullOrWhiteSpace(RequestedCode)
                ? RequestedCode!
                : (Team.TeamCode ?? Team.TeamName ?? "Equipe");
        }
    }
}
