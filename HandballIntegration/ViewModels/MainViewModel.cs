using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
namespace HandballIntegration.ViewModels
{
    public partial class MainViewModel : ObservableObject   
    {
        [ObservableProperty]
        private bool isApiConnected;
    }
}
