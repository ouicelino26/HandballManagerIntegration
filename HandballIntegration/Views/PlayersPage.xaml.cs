using HandballIntegration.Services;
using HandballManagerCore.DTO;
using HandballManagerCore.Models;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HandballIntegration.Views
{
    public partial class PlayersPage : System.Windows.Controls.Page
    {
        private readonly PlayersApiService _playersService;
        private List<PlayerDto> _allPlayers;

        public PlayersPage()
        {
            InitializeComponent();

            _playersService = App.Services.GetRequiredService<PlayersApiService>();

            Loaded += PlayersPage_Loaded;
        }

        private async void PlayersPage_Loaded(object sender, RoutedEventArgs e)
        {
            _allPlayers = await _playersService.GetPlayersAsync() ?? new List<PlayerDto>();
            PlayersGrid.ItemsSource = _allPlayers;
        }

       
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.ToLower();

            var filtered = _allPlayers.Where(p =>
                (p.FullName ?? "").ToLower().Contains(query) ||
                (p.TeamName ?? "").ToLower().Contains(query) ||
                (p.PositionName ?? "").ToLower().Contains(query) ||
                (p.CountryName ?? "").ToLower().Contains(query)
            ).ToList();

            PlayersGrid.ItemsSource = filtered;
        }

        // ACTIONS
     private async void EditPlayer_Click(object sender, RoutedEventArgs e)
{
 
    var player = (sender as FrameworkElement)?.DataContext as PlayerDto;
    if (player == null) return;

  
    var dialog = (ContentDialog)this.Resources["EditPlayerDialog"];

  dialog.DataContext = player;

    var result = await dialog.ShowAsync();

       if (result == ContentDialogResult.Primary)
    {
               MessageBox.Show($"Modifications enregistrées pour : {player.FullName}");
    }
    else
    {
        
        MessageBox.Show("Modification annulée");
    }
}


        private void DeletePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PlayerDto player)
                MessageBox.Show($"Supprimer : {player.FullName}");
        }
    }
}
