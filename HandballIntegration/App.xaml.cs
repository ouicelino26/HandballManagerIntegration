using HandballIntegration.Data;
using HandballIntegration.Services;
using HandballIntegration.ViewModels;
using HandballIntegration.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;

namespace HandballIntegration
{
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        public static IServiceProvider Services => AppHost.Services;

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<ApiSettings>(context.Configuration.GetSection("ApiSettings"));
                    services.AddHttpClient<ApiService>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["ApiSettings:BaseUrl"]);
                    });

                    services.AddHttpClient();
                    services.AddSingleton<IApiAuthService, ApiAuthService>();
                    services.AddSingleton<ApiService>();
                    services.AddSingleton<PlayersApiService>();
                    services.AddSingleton<UsersApiService>();
                    services.AddSingleton<IntegrationViewModel>();
                    services.AddSingleton<TimeIntegrationViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            base.OnStartup(e);
            await AppHost.StartAsync();

            var authService = Services.GetRequiredService<IApiAuthService>();
            authService.Logout();

            var loginWindow = new LoginWindow();
            var loginResult = loginWindow.ShowDialog();

            if (loginResult != true)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            AppHost.Dispose();
            base.OnExit(e);
        }
    }
}
