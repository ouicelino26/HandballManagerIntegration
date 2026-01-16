using HandballIntegration.Data;
using HandballIntegration.Services;
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
                    services.AddSingleton<IntegrationViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();
            base.OnStartup(e);
        }
    }
}
