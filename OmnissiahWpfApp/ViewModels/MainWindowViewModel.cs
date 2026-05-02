using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmnissiahWpfApp.Models;
using OmnissiahWpfApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OmnissiahWpfApp.ViewModels
{
    public sealed class MainWindowViewModel : ObservableObject {

        TcpServerService _server;
        TcpClientService _client;
        SignalAggregatorService _signalAggregator;

        public ObservableCollection<SignalAggregatedRecordModel> Records { get; set; } = new();

        public bool IsServerRunning => _server.IsRunning;
        public bool IsClientConnected => _client.IsConnected;
        public string ServerButtonText => _server.IsRunning ? "Stop server" : "Start server";
        public string ClientButtonText => _client.IsConnected ? "Stop client" : "Start client";
        public Visibility IsClientButtonVisible => _server.IsRunning || _client.IsConnected ? Visibility.Visible : Visibility.Collapsed;

        public IAsyncRelayCommand ToggleServerCommand { get; }
        public IAsyncRelayCommand ToggleClientCommand { get; }


        public MainWindowViewModel(TcpServerService server, TcpClientService client, SignalAggregatorService signalAggregator) 
        {
            _server = server;
            _client = client;
            _signalAggregator = signalAggregator;

            ToggleServerCommand = new AsyncRelayCommand(ToggleServer);
            ToggleClientCommand = new AsyncRelayCommand(ToggleClient);

            _signalAggregator.OnRecordCreated += SignalAggregator_OnRecordCreated;
            _client.OnConnectionStateChanged += RefreshButtonsState;
            _server.OnConnectionStateChanged += RefreshButtonsState;
        }

        private async Task ToggleServer() 
        {
            if (_server.IsRunning) 
                await StopServer();
            else 
                await StartServer();

            RefreshButtonsState();
        }

        private async Task ToggleClient() 
        {
            if (_client.IsConnected) 
                await StopClient();
            else 
                await StartClient();

            RefreshButtonsState();
        }

        private async Task StartServer() => await _server.StartAsync();

        private async Task StopServer() => await _server.StopAsync();

        private async Task StartClient() => await _client.StartAsync();
        
        private async Task StopClient() => await _client.StopAsync();
        

        private void RefreshButtonsState() {

            ToggleServerCommand.NotifyCanExecuteChanged();
            ToggleClientCommand.NotifyCanExecuteChanged();

            OnPropertyChanged(nameof(IsServerRunning));
            OnPropertyChanged(nameof(IsClientConnected));
            OnPropertyChanged(nameof(ServerButtonText));
            OnPropertyChanged(nameof(ClientButtonText));
            OnPropertyChanged(nameof(IsClientButtonVisible));
        }

        private void SignalAggregator_OnRecordCreated(SignalAggregatedRecordModel record) {

            Application.Current.Dispatcher.Invoke(() => Records.Insert(0, record));

            _ = Task.Delay(1400).ContinueWith(_ => Application.Current.Dispatcher.Invoke(() => record.IsNew = false));
        }
    }
}
