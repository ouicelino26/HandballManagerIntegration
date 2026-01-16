using HandballIntegration.Services;
using HandballIntegration.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using System;
using System.Windows;


namespace HandballIntegration
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;

        public MainWindow()
        {
            InitializeComponent();
           
            _apiService = App.Services.GetRequiredService<ApiService>();
            DataContext = new MainViewModel();
            Loaded += MainWindow_Loaded;
        }

        private void NavigationView_SelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag)
                {
                    case "dashboard":
                        ContentFrame.Navigate(new Views.DashboardPage());
                        break;

                    case "integration":
                        ContentFrame.Navigate(new Views.IntegrationPage());
                        break;

                    case "players":
                        ContentFrame.Navigate(new Views.PlayersPage());
                        break;
                    case "Pdf":
                        ContentFrame.Navigate(new Views.SendPdf());
                        break;

                }
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            bool success = await _apiService.TestConnectionAsync();

            
            var vm = DataContext as MainViewModel;
            vm.IsApiConnected = success;

            
        }
    }
}
