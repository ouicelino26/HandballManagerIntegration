using HandballManagerCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HandballIntegration.Views
{
    /// <summary>
    /// Logique d'interaction pour FoundPlayersWindows.xaml
    /// </summary>
    public partial class FoundPlayersWindows : Window
    {
        public List<Player> Players { get; }
        public string SearchText { get; }
        public Player? SelectedPlayer { get; private set; }

        public FoundPlayersWindows(List<Player> players, string searchText)
        {
            InitializeComponent();
            Players = players;
            SearchText = searchText;
            DataContext = this;
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            SelectedPlayer = PlayersListBox.SelectedItem as Player;
            if (SelectedPlayer == null)
            {
                MessageBox.Show("Veuillez sélectionner une joueuse.");
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
