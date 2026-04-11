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
        private List<PlayerDto> _allPlayers = new();

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
                _allPlayers = await _playersService.GetPlayersAsync() ?? new List<PlayerDto>();
                ApplyFilter();
            }
            catch (System.Exception ex)
            {
                _allPlayers = new List<PlayerDto>();
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

        private void UpdateSummary(IReadOnlyCollection<PlayerDto> filteredPlayers)
        {
            PlayersCountText.Text = _allPlayers.Count.ToString();
            ResultsCountText.Text = filteredPlayers.Count.ToString();
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
            if ((sender as FrameworkElement)?.DataContext is not PlayerDto player)
            {
                return;
            }

            var dialog = (ModernWpf.Controls.ContentDialog)Resources["EditPlayerDialog"];
            dialog.DataContext = player;

            var result = await dialog.ShowAsync();

            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                MessageBox.Show($"Les modifications locales pour {player.FullName} ont bien ete prises en compte.", "Edition", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeletePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not PlayerDto player)
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
    }
}
