using HandballIntegration.Presentation.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace HandballIntegration.Presentation.Views
{
    public partial class LegacyIntegrationView : UserControl
    {
        public LegacyIntegrationView()
        {
            InitializeComponent();
        }

        private LegacyIntegrationViewModel? Vm => DataContext as LegacyIntegrationViewModel;

        private void BrowseClassicFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Selectionnez un dossier contenant les fichiers du match"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Vm?.ClassicViewModel.LoadFiles(dialog.FileName);
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
                Vm?.TimeViewModel.LoadFiles(dialog.FileName);
            }
        }

        private async void Integration_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is MatchToIntegrate file)
            {
                await (Vm?.ClassicViewModel.IntegrateFileAsync(file) ?? Task.CompletedTask);
            }
        }

        private async void TimeIntegration_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is TimePlayersFileToIntegrate file)
            {
                await (Vm?.TimeViewModel.IntegrateFileAsync(file) ?? Task.CompletedTask);
            }
        }
    }
}
