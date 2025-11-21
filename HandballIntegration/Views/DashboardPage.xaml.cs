using System.Collections.Generic;
using System.Windows.Controls;

namespace HandballIntegration.Views
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            // Temporaire : FAKE DATA (remplacé après)
            var data = new List<object>()
            {
                new { Type="Joueuse", Nom="Marie Dubois", Date="2025-11-21" },
                new { Type="Match", Nom="Lille vs Dunkerque", Date="2025-11-20" },
                new { Type="Équipe", Nom="U18 Féminine", Date="2025-11-19" }
            };

            RecentDataGrid.ItemsSource = data;
        }
    }
}
