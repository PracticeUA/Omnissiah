using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OmnissiahWpfApp.Services;
using OmnissiahWpfApp.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

namespace OmnissiahWpfApp 
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application 
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e) 
        {
            _host = Host.CreateDefaultBuilder().ConfigureServices(services => 
            {
                services.AddSingleton<SignalAggregatorService>();

                services.AddSingleton<TcpServerService>();
                services.AddSingleton<TcpClientService>();

                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<MainWindow>();
            }).Build();

            await _host.StartAsync();

            var window = _host.Services.GetRequiredService<MainWindow>();

            window.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e) 
        {
            if (_host != null) 
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            base.OnExit(e);
        }
    }
}
