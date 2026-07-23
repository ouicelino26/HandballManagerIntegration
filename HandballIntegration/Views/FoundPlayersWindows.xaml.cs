using HandballManagerCore.DTO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace HandballIntegration.Views
{
    public partial class FoundPlayersWindows : Window
    {
        public List<PlayerListItemDto> Players { get; }
        public string SearchText { get; }
        public PlayerListItemDto? SelectedPlayer { get; private set; }

        public FoundPlayersWindows(List<PlayerListItemDto> players, string searchText)
        {
            InitializeComponent();
            Players = players;
            SearchText = searchText;
            DataContext = this;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            SelectedPlayer = PlayersListBox.SelectedItem as PlayerListItemDto;
            if (SelectedPlayer == null)
            {
                MessageBox.Show("Veuillez selectionner une joueuse.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PlayersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlayersListBox.SelectedItem != null)
            {
                Validate_Click(sender, e);
            }
        }
    }
}
