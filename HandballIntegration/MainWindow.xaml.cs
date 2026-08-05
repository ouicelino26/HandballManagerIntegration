using System.Windows;
using HandballIntegration.Presentation.ViewModels;
using HandballIntegration.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HandballIntegration;

public partial class MainWindow : Window
{
    private readonly AdminShellViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<AdminShellViewModel>();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
        _viewModel.LogoutRequested += OnLogoutRequested;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await _viewModel.InitializeAsync();

    private async void OnLogoutRequested(object? sender, EventArgs e)
    {
        Hide();
        var loginResult = new LoginWindow().ShowDialog();
        if (loginResult != true)
        {
            Close();
            return;
        }

        Show();
        Activate();
        await _viewModel.InitializeAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.LogoutRequested -= OnLogoutRequested;
        _viewModel.Dispose();
    }
}
