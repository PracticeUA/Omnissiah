using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmnissiahWpfApp.Models;
using OmnissiahWpfApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace OmnissiahWpfApp.ViewModels
{
    /// <summary>
    /// View model for the main application window.
    /// Manages TCP server and client lifecycle, incoming signal records,
    /// and collection filtering for the data grid.
    /// </summary>
    /// <remarks>
    /// Depends on three services injected via constructor:
    /// <list type="bullet">
    ///   <item><see cref="TcpServerService"/> — runs the TCP listener.</item>
    ///   <item><see cref="TcpClientService"/> — manages the outgoing TCP connection.</item>
    ///   <item><see cref="SignalAggregatorService"/> — aggregates raw signal records and raises <see cref="SignalAggregatorService.OnRecordCreated"/>.</item>
    /// </list>
    ///
    /// Filtering is implemented via <see cref="ICollectionView"/> over <see cref="Records"/>.
    /// Frequency, bandwidth, and SNR filters are populated dynamically as new records arrive.
    /// Timestamp and count filters use static option sets.
    /// All five filters are applied simultaneously as a logical AND.
    ///
    /// Button colors reflect connection state via <see cref="IsServerRunning"/>
    /// and <see cref="IsClientConnected"/> — consumed by DataTriggers in the resource dictionary.
    /// New records flash blue for 1.4 seconds via <see cref="SignalAggregatedRecordModel.IsNew"/>.
    /// </remarks>
    public sealed class MainWindowViewModel : ObservableObject {

        #region * PARMS & PROPS *

        TcpServerService _server;
        TcpClientService _client;
        SignalAggregatorService _signalAggregator;

        public ObservableCollection<SignalAggregatedRecordModel> Records { get; set; } = new();
        public ICollectionView RecordsView { get; }

        public ObservableCollection<string> FrequencyOptions { get; } = new() { "All" };
        public ObservableCollection<string> BandwidthOptions { get; } = new() { "All" };
        public ObservableCollection<string> SnrOptions { get; } = new() { "All" };
        public IReadOnlyList<string> TimestampOptions { get; } = new[] { "All", "Last 1 min", "Last 3 min", "Last 5 min", "Last 10 min", "Last 30 min" };
        public IReadOnlyList<string> CountOptions { get; } = new[] { "All", "> 10", "> 20", "> 50", "> 100" };

        private string _filterTimestamp = "All";
        private string _filterFrequency = "All";
        private string _filterBandwidth = "All";
        private string _filterSnr = "All";
        private string _filterCount = "All";

        public string FilterTimestamp
        {
            get => _filterTimestamp;
            set 
            { 
                SetProperty(ref _filterTimestamp, value); 
                RecordsView.Refresh(); 
            }
        }
        public string FilterFrequency
        {
            get => _filterFrequency;
            set 
            { 
                SetProperty(ref _filterFrequency, value); 
                RecordsView.Refresh(); 
            }
        }
        public string FilterBandwidth
        {
            get => _filterBandwidth;
            set 
            { 
                SetProperty(ref _filterBandwidth, value); 
                RecordsView.Refresh(); 
            }
        }
        public string FilterSnr
        {
            get => _filterSnr;
            set 
            { 
                SetProperty(ref _filterSnr, value); 
                RecordsView.Refresh(); 
            }
        }
        public string FilterCount
        {
            get => _filterCount;
            set
            {
                SetProperty(ref _filterCount, value);
                RecordsView.Refresh();
            }
        }

        public bool IsServerRunning => _server.IsRunning;
        public bool IsClientConnected => _client.IsConnected;
        public string ServerButtonText => _server.IsRunning ? "Stop server" : "Start server";
        public string ClientButtonText => _client.IsConnected ? "Stop client" : "Start client";
        public bool IsClientButtonEnabled => _server.IsRunning ? true : false;

        #endregion

        #region * COMMANDS *

        public IAsyncRelayCommand ToggleServerCommand { get; }
        public IAsyncRelayCommand ToggleClientCommand { get; }
        public IRelayCommand ResetFiltersCommand { get; }

        #endregion

        #region * CONSTRUCTOR *

        public MainWindowViewModel(TcpServerService server, TcpClientService client, SignalAggregatorService signalAggregator) {

            _server = server;
            _client = client;
            _signalAggregator = signalAggregator;

            RecordsView = CollectionViewSource.GetDefaultView(Records);
            RecordsView.Filter = FilterRecord;

            ToggleServerCommand = new AsyncRelayCommand(ToggleServer);
            ToggleClientCommand = new AsyncRelayCommand(ToggleClient);
            ResetFiltersCommand = new RelayCommand(ResetFilters);

            _signalAggregator.OnRecordCreated += SignalAggregator_OnRecordCreated;
            _client.OnConnectionStateChanged += RefreshButtonsState;
            _server.OnConnectionStateChanged += RefreshButtonsState;
        }

        #endregion

        #region * METHODS*

        #region * FILTERING *

        private bool FilterRecord(object obj)
        {
            if (obj is not SignalAggregatedRecordModel r) return false;

            if (_filterTimestamp != "All") {

                var minutes = int.Parse(_filterTimestamp.Replace("Last ", "").Replace(" min", ""));
                var threshold = DateTimeOffset.UtcNow.AddMinutes(-minutes);
                if (r.Timestamp < threshold)
                    return false;
            }

            if (_filterFrequency != "All" && r.FrequencyMHz.ToString() != _filterFrequency) 
                return false;

            if (_filterBandwidth != "All" && r.BandwidthKHz.ToString() != _filterBandwidth) 
                return false;

            if (_filterSnr != "All" && r.SnrDb.ToString() != _filterSnr) 
                return false;

            if (_filterCount != "All") {

                var threshold = int.Parse(_filterCount.Replace("> ", ""));
                if (r.Count < threshold) 
                    return false;
            }

            return true;
        }

        private static void AddFilterOption(ObservableCollection<string> options, string value) {

            if (!options.Contains(value))
                options.Add(value);
        }

        private void ResetFilters() {

            _filterTimestamp = "All";
            _filterFrequency = "All";
            _filterBandwidth = "All";
            _filterSnr = "All";
            _filterCount = "All";

            OnPropertyChanged(nameof(FilterTimestamp));
            OnPropertyChanged(nameof(FilterFrequency));
            OnPropertyChanged(nameof(FilterBandwidth));
            OnPropertyChanged(nameof(FilterSnr));
            OnPropertyChanged(nameof(FilterCount));

            RecordsView.Refresh();
        }

        #endregion

        #region * SERVER & CLIENT *

        private async Task ToggleServer() {

            if (_server.IsRunning) 
                await StopServer();
            else 
                await StartServer();

            RefreshButtonsState();
        }

        private async Task ToggleClient() {

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
            OnPropertyChanged(nameof(IsClientButtonEnabled));
        }

        #endregion

        #region * NEW RECORD RECEIVED *

        private void SignalAggregator_OnRecordCreated(SignalAggregatedRecordModel record) {

            Application.Current.Dispatcher.Invoke(() =>
            {
                Records.Insert(0, record);

                AddFilterOption(FrequencyOptions, record.FrequencyMHz.ToString());
                AddFilterOption(BandwidthOptions, record.BandwidthKHz.ToString());
                AddFilterOption(SnrOptions, record.SnrDb.ToString());

                RecordsView.Refresh();
            });

            _ = Task.Delay(1400).ContinueWith(_ => Application.Current.Dispatcher.Invoke(() => record.IsNew = false));
        }

        #endregion

        #endregion
    }
}
