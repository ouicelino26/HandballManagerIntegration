using System.Windows;
using HandballIntegration.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HandballIntegration.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<LoginViewModel>();
        DataContext = _viewModel;
        _viewModel.LoginSucceeded += OnLoginSucceeded;
        Loaded += (_, _) => UsernameTextBox.Focus();
        Closed += OnClosed;
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnClosed(object? sender, EventArgs e) => _viewModel.LoginSucceeded -= OnLoginSucceeded;
}
