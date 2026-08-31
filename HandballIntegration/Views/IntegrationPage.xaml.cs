using HandballIntegration.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace HandballIntegration.Views
{
    public partial class IntegrationPage : Page
    {
        public IntegrationViewModel ClassicViewModel { get; }
        public TimeIntegrationViewModel TimeViewModel { get; }

        public IntegrationPage()
        {
            InitializeComponent();
            ClassicViewModel = App.Services.GetRequiredService<IntegrationViewModel>();
            TimeViewModel = App.Services.GetRequiredService<TimeIntegrationViewModel>();
            DataContext = this;
        }

        private void BrowseClassicFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Selectionnez un dossier contenant les fichiers du match"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ClassicViewModel.LoadFiles(dialog.FileName);
            }
        }

        private void BrowseTimeFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Selectionnez un dossier contenant les fichiers de temps de jeu"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TimeViewModel.LoadFiles(dialog.FileName);
            }
        }

        private async void Integration_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is MatchToIntegrate file)
            {
                await ClassicViewModel.IntegrateFileAsync(file);
            }
        }

        private async void TimeIntegration_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is TimePlayersFileToIntegrate file)
            {
                await TimeViewModel.IntegrateFileAsync(file);
            }
        }

        private async void IntegrateTous_Click(object sender, RoutedEventArgs e)
        {
            await ClassicViewModel.IntegrateTousAsync();
        }

        private async void IntegrateTousTime_Click(object sender, RoutedEventArgs e)
        {
            await TimeViewModel.IntegrateTousAsync();
        }
    }
}
