using HandballIntegration.Services;
using HandballManagerCore.DTO;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HandballIntegration.Views
{
    public partial class PlayersPage : Page
    {
        private readonly PlayersApiService _playersService;
        private List<PlayerListItemDto> _allPlayers = new();
        private List<TeamDto> _teams = new();
        private List<LookupItemDto> _positions = new();
        private List<LookupItemDto> _nationalities = new();

        public PlayersPage()
        {
            InitializeComponent();

            _playersService = App.Services.GetRequiredService<PlayersApiService>();
            Loaded += PlayersPage_Loaded;
        }

        private async void PlayersPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _teams = await _playersService.GetTeamsAsync();
                _positions = await _playersService.GetPositionsAsync();
                _nationalities = await _playersService.GetNationalitiesAsync();
                _allPlayers = await _playersService.GetPlayersAsync() ?? new List<PlayerListItemDto>();
                ApplyFilter();
            }
            catch (System.Exception ex)
            {
                _allPlayers = new List<PlayerListItemDto>();
                ApplyFilter();
                MessageBox.Show(
                    "Impossible de charger les joueuses : " + ex.Message,
                    "Chargement",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string query = SearchBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            var filtered = _allPlayers.Where(player =>
                string.IsNullOrWhiteSpace(query) ||
                (player.FullName ?? string.Empty).ToLowerInvariant().Contains(query) ||
                (player.TeamName ?? string.Empty).ToLowerInvariant().Contains(query) ||
                (player.PositionName ?? string.Empty).ToLowerInvariant().Contains(query) ||
                (player.CountryName ?? string.Empty).ToLowerInvariant().Contains(query))
                .ToList();

            PlayersGrid.ItemsSource = filtered;
            UpdateSummary(filtered);
        }

        private void UpdateSummary(IReadOnlyCollection<PlayerListItemDto> filteredPlayers)
        {
            PlayersCountText.Text = _allPlayers.Count.ToString();
            ResultsCountText.Text = filteredPlayers.Count(player => player.IsActive).ToString();
            TeamsCountText.Text = filteredPlayers
                .Select(player => player.TeamName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Count()
                .ToString();

            ResultsSummaryText.Text = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? $"{filteredPlayers.Count} profils visibles"
                : $"{filteredPlayers.Count} resultat(s) pour \"{SearchBox.Text.Trim()}\"";
        }

        private async void EditPlayer_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not PlayerListItemDto player)
            {
                return;
            }

            var editable = new EditablePlayerState
            {
                PlayerId = player.Id,
                FirstName = ExtractFirstName(player.FullName),
                LastName = ExtractLastName(player.FullName),
                Birthday = player.Birthday,
                Age = player.Age,
                Number = player.Number,
                TeamId = player.TeamId,
                PositionId = player.PositionId,
                NationalityId = FindNationalityId(player.CountryName),
                IsActive = player.IsActive,
                Teams = _teams,
                Positions = _positions,
                Nationalities = _nationalities
            };

            var dialog = (ModernWpf.Controls.ContentDialog)Resources["EditPlayerDialog"];
            dialog.DataContext = editable;

            var result = await dialog.ShowAsync();

            if (result != ModernWpf.Controls.ContentDialogResult.Primary)
            {
                return;
            }

            if (!editable.IsValid(out var validationMessage))
            {
                MessageBox.Show(validationMessage, "Edition", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool success = await _playersService.UpdatePlayerAsync(new PlayersApiService.PlayerEditionRequest
            {
                PlayerId = player.Id,
                FirstName = editable.FirstName.Trim(),
                LastName = editable.LastName.Trim(),
                Birthday = editable.Birthday,
                Age = editable.Age,
                Number = editable.Number,
                TeamId = editable.TeamId,
                PositionId = editable.PositionId,
                NationalityId = editable.NationalityId,
                IsActive = editable.IsActive
            });

            if (!success)
            {
                MessageBox.Show("La mise a jour de la joueuse a echoue cote API.", "Edition", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            player.FullName = $"{editable.FirstName.Trim()} {editable.LastName.Trim()}".Trim();
            player.Birthday = editable.Birthday;
            player.Age = editable.Age;
            player.Number = editable.Number;
            player.TeamId = editable.TeamId;
            player.TeamName = _teams.FirstOrDefault(team => team.Id == editable.TeamId)?.Name;
            player.PositionId = editable.PositionId;
            player.PositionName = _positions.FirstOrDefault(position => position.Id == editable.PositionId)?.Name;
            player.Nationality = _nationalities.FirstOrDefault(nationality => nationality.Id == editable.NationalityId)?.Name;
            player.IsActive = editable.IsActive;
            ApplyFilter();
            MessageBox.Show($"La fiche de {player.FullName} a ete mise a jour.", "Edition", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeletePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not PlayerListItemDto player)
            {
                return;
            }

            var result = MessageBox.Show(
                $"Supprimer la joueuse : {player.FullName} ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            bool success = await _playersService.DeletePlayerAsync(player.Id);

            if (success)
            {
                _allPlayers.Remove(player);
                ApplyFilter();
                MessageBox.Show("La joueuse a ete supprimee avec succes.", "Suppression", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show("La suppression a echoue cote API.", "Suppression", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private sealed class EditablePlayerState
        {
            public int PlayerId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public System.DateTime? Birthday { get; set; }
            public int? Age { get; set; }
            public int? Number { get; set; }
            public int? TeamId { get; set; }
            public int? PositionId { get; set; }
            public int? NationalityId { get; set; }
            public bool IsActive { get; set; }
            public List<TeamDto> Teams { get; set; } = new();
            public List<LookupItemDto> Positions { get; set; } = new();
            public List<LookupItemDto> Nationalities { get; set; } = new();

            public bool IsValid(out string message)
            {
                if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
                {
                    message = "Le prenom et le nom sont obligatoires.";
                    return false;
                }

                if (!TeamId.HasValue || !PositionId.HasValue || !NationalityId.HasValue)
                {
                    message = "Equipe, poste et nationalite sont obligatoires.";
                    return false;
                }

                message = string.Empty;
                return true;
            }
        }

        private static string ExtractFirstName(string? fullName)
        {
            var normalized = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            int separator = normalized.IndexOf(' ');
            return separator < 0 ? normalized : normalized[..separator].Trim();
        }

        private static string ExtractLastName(string? fullName)
        {
            var normalized = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            int separator = normalized.IndexOf(' ');
            return separator < 0 ? string.Empty : normalized[(separator + 1)..].Trim();
        }

        private int? FindNationalityId(string? nationalityName)
        {
            if (string.IsNullOrWhiteSpace(nationalityName))
            {
                return null;
            }

            return _nationalities
                .FirstOrDefault(item => string.Equals(item.Name?.Trim(), nationalityName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                ?.Id;
        }
    }
}
