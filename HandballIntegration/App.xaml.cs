using HandballIntegration.Data;
using HandballIntegration.Services;
using HandballIntegration.ViewModels;
using HandballIntegration.Views;
using HandballIntegration.Admin.Abstractions;
using HandballIntegration.Admin.Services;
using HandballIntegration.Admin.Workflows;
using HandballIntegration.Core.Abstractions;
using HandballIntegration.Infrastructure.Api;
using HandballIntegration.Infrastructure.Files;
using HandballIntegration.Presentation.ViewModels;
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
                    services.AddSingleton(serviceProvider =>
                        serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>().Value);
                    services.AddHttpClient<ApiService>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["ApiSettings:ApiBaseUrl"]!);
                        client.Timeout = TimeSpan.FromSeconds(
                            context.Configuration.GetValue("ApiSettings:TimeoutSeconds", 30));
                    });

                    services.AddSingleton<IClock, SystemClock>();
                    services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
                    services.AddSingleton<IAdminSessionStorage, MemoryAdminSessionStorage>();
                    services.AddSingleton<IAdminSessionService, AdminSessionService>();
                    services.AddSingleton<IProblemDetailsMapper, AdminProblemDetailsMapper>();
                    services.AddSingleton<IAdminNavigationService, AdminNavigationService>();
                    services.AddTransient<AdminSessionHandler>();
                    services.AddHttpClient<IAdminApiClient, AdminApiClient>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["ApiSettings:ApiBaseUrl"]!);
                        client.Timeout = TimeSpan.FromSeconds(
                            context.Configuration.GetValue("ApiSettings:TimeoutSeconds", 30));
                    }).AddHttpMessageHandler<AdminSessionHandler>();
                    services.AddSingleton<IAdminCapabilitiesService, AdminCapabilitiesService>();

                    services.AddHttpClient<IAdminApiTransport, AdminApiTransport>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["ApiSettings:ApiBaseUrl"]!);
                        client.Timeout = TimeSpan.FromSeconds(
                            context.Configuration.GetValue("ApiSettings:TimeoutSeconds", 30));
                    }).AddHttpMessageHandler<AdminSessionHandler>();
                    services.AddTransient<IAdminDashboardApiClient, AdminDashboardApiClient>();
                    services.AddTransient<IAdminImportApiClient, AdminImportApiClient>();
                    services.AddTransient<IAdminMatchApiClient, AdminMatchApiClient>();
                    services.AddTransient<IAdminEventApiClient, AdminEventApiClient>();
                    services.AddTransient<IAdminPlayerApiClient, AdminPlayerApiClient>();
                    services.AddTransient<IAdminTeamApiClient, AdminTeamApiClient>();
                    services.AddTransient<IAdminReferenceDataApiClient, AdminReferenceDataApiClient>();
                    services.AddSingleton<IAdminDataQualityApiClient, AdminDataQualityApiClient>();
                    services.AddTransient<IAdminAuditApiClient, AdminAuditApiClient>();
                    services.AddTransient<IAdminMaintenanceApiClient, AdminMaintenanceApiClient>();
                    services.AddTransient<IAdminUsersApiClient, AdminUsersApiClient>();
                    services.AddSingleton<IFilePickerService, WpfFilePickerService>();
                    services.AddSingleton<IAdminModuleFactory, AdminModuleFactory>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ImportsViewModel>();
                    services.AddTransient<MatchesViewModel>();
                    services.AddTransient<PlayersAdminViewModel>();
                    services.AddTransient<TeamsAdminViewModel>();
                    services.AddTransient<ReferenceDataViewModel>();
                    services.AddTransient<AuditViewModel>();
                    services.AddTransient<UsersAdminViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<AdminShellViewModel>();
                    services.AddTransient<LoginViewModel>();

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

            if (AdminStartupDecision.ShouldShutdownAfterLogin(loginResult))
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
