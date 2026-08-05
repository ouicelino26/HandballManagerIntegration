using HandballIntegration.Services;
using HandballManagerCore.DTO;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace HandballIntegration.Views
{
    public partial class DashboardPage : Page
    {
        private readonly ApiService _apiService;
        private readonly PlayersApiService _playersService;

        public DashboardPage()
        {
            InitializeComponent();

            _apiService = App.Services.GetRequiredService<ApiService>();
            _playersService = App.Services.GetRequiredService<PlayersApiService>();
            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                SyncMomentText.Text = $"Point de controle : {DateTime.Now.ToString("dddd d MMMM yyyy - HH:mm", CultureInfo.GetCultureInfo("fr-FR"))}";

                bool isConnected = await _apiService.TestConnectionAsync();
                ApplyApiStatus(isConnected);

                var players = await _playersService.GetPlayersAsync() ?? new List<PlayerListItemDto>();
                PopulateMetrics(players);
                PopulatePreview(players);
            }
            catch (Exception ex)
            {
                ApplyApiStatus(false);
                SyncMomentText.Text = "Chargement interrompu";
                DashboardHintText.Text = "Le dashboard est reste ouvert, mais une erreur s'est produite pendant le chargement : " + ex.Message;
                PopulateMetrics(new List<PlayerListItemDto>());
                PopulatePreview(new List<PlayerListItemDto>());
            }
        }

        private void ApplyApiStatus(bool isConnected)
        {
            if (isConnected)
            {
                ApiStatusBadge.Background = new SolidColorBrush(Color.FromRgb(220, 239, 233));
                ApiStatusText.Foreground = new SolidColorBrush(Color.FromRgb(33, 93, 80));
                ApiStatusText.Text = "API locale confirmee";
                DashboardHintText.Text = "L'authentification repond correctement. Le poste est pret pour enchainer integration, controle base et restitution.";
                return;
            }

            ApiStatusBadge.Background = new SolidColorBrush(Color.FromRgb(247, 221, 215));
            ApiStatusText.Foreground = new SolidColorBrush(Color.FromRgb(184, 74, 53));
            ApiStatusText.Text = "Connexion indisponible";
            DashboardHintText.Text = "L'application reste navigable, mais les vues alimentees par l'API auront des contenus partiels tant que l'authentification ne repond pas.";
        }

        private void PopulateMetrics(IReadOnlyCollection<PlayerListItemDto> players)
        {
            PlayersCountText.Text = players.Count.ToString();
            TeamsCountText.Text = players
                .Select(p => p.TeamName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Count()
                .ToString();
            CountriesCountText.Text = players
                .Select(p => p.Nationality)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Count()
                .ToString();
        }

        private void PopulatePreview(IReadOnlyCollection<PlayerListItemDto> players)
        {
            RecentDataGrid.ItemsSource = players
                .Where(p => !string.IsNullOrWhiteSpace(p.FullName))
                .OrderBy(p => p.TeamName)
                .ThenBy(p => p.FullName)
                .Take(8)
                .Select(p => new
                {
                    Joueuse = p.FullName,
                    Equipe = string.IsNullOrWhiteSpace(p.TeamName) ? "Non renseignee" : p.TeamName,
                    Poste = string.IsNullOrWhiteSpace(p.PositionName) ? "Non renseigne" : p.PositionName,
                    Pays = string.IsNullOrWhiteSpace(p.Nationality) ? "Non renseigne" : p.Nationality
                })
                .ToList();
        }
    }
}
