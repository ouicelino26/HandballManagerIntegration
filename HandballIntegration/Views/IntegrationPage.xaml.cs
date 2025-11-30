using HandballManagerCore.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;
using HandballManagerCore.DTO;

namespace HandballIntegration.Views
{
    public partial class IntegrationPage : Page
    {
        public IntegrationViewModel ViewModel { get; } = new IntegrationViewModel();

        public IntegrationPage()
        {
            InitializeComponent();
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
    }



public class MatchToIntegrate
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }

       
        public MatchDto MatchInfo { get; set; } = new MatchDto();
    }

}
