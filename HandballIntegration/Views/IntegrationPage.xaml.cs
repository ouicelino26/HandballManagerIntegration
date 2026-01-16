using HandballManagerCore.DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HandballIntegration.Views
{
    public partial class IntegrationPage : Page
    {
        public IntegrationViewModel ViewModel { get; } = new IntegrationViewModel();

        public IntegrationPage()
        {
            InitializeComponent();
            ViewModel = App.Services.GetRequiredService<IntegrationViewModel>();
            DataContext = ViewModel;
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Sélectionnez un dossier contenant les fichiers du match"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ViewModel.LoadFiles(dialog.FileName);
            }
        }

        private async void Integration_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is MatchToIntegrate file)
            {
                await ViewModel.IntegrateFileAsync(file);
            }
        }
    }
}
