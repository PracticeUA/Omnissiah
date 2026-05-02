using OmnissiahWpfApp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OmnissiahWpfApp.Services
{
    /// <summary>
    /// TCP client that connects to a signal stream server, reads binary frames,
    /// and forwards parsed records to <see cref="SignalAggregatorService"/>.
    /// </summary>
    /// <remarks>
    /// Lifecycle is controlled via <see cref="StartAsync"/> and <see cref="StopAsync"/>.
    /// On successful connection a background read loop is started on a separate task.
    /// If the connection attempt fails, a <see cref="MessageBox"/> is shown and
    /// <see cref="IsConnected"/> remains <c>false</c>.
    ///
    /// Frame reading follows a length-prefixed protocol:
    /// <code>
    /// [0..2)  uint16  Payload length in bytes (little-endian)
    /// [2..N)  byte[]  Raw signal payload, parsed by <see cref="FrameParser"/>
    /// </code>
    /// Partial reads are handled correctly — the payload loop accumulates bytes
    /// until the full frame is received before parsing.
    ///
    /// The read loop exits on cancellation, stream EOF, or any socket exception,
    /// setting <see cref="IsConnected"/> to <c>false</c> and raising
    /// <see cref="OnConnectionStateChanged"/> so the UI reflects the disconnection
    /// even if it was caused by a remote server shutdown.
    /// </remarks>
    public sealed class TcpClientService {

        private readonly SignalAggregatorService _signalAggregator;
        private readonly FrameParser _frameParser = new();
        private TcpClient _tcpClient;
        private CancellationTokenSource _cts;

        public event Action? OnConnectionStateChanged;

        public bool IsConnected { get; private set; }

        public TcpClientService(SignalAggregatorService signalAggregator) {

            _signalAggregator = signalAggregator;
        }

        public async Task StartAsync(string host = "127.0.0.1", int port = 4444) {

            if (IsConnected) 
                return;

            _cts = new CancellationTokenSource();

            _tcpClient = new TcpClient();

            try {

                await _tcpClient.ConnectAsync(host, port);
            }
            catch (Exception ex) {

                IsConnected = false;
                OnConnectionStateChanged?.Invoke();
                MessageBox.Show($"Failed to connect to server: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //await _tcpClient.ConnectAsync(host, port);

            IsConnected = true;
            OnConnectionStateChanged?.Invoke();

            _ = ReadLoop(_cts.Token);
        }

        public async Task StopAsync() {

            if (!IsConnected) 
                return;

            _cts?.Cancel();
            _tcpClient?.Close();
            IsConnected = false;
            OnConnectionStateChanged?.Invoke();

            await Task.CompletedTask;
        }

        public async Task ReadLoop(CancellationToken token) {

            await using var stream = _tcpClient.GetStream();

            while (!token.IsCancellationRequested) {

                var header = new byte[2];

                try {

                    var readHeader = await stream.ReadAsync(header, token);
                    if (readHeader == 0)
                        break;

                    var lenght = header[0] | (header[1] << 8);

                    var payload = new byte[lenght];

                    var total = 0;

                    while (total < lenght) {

                        var readPayload = await stream.ReadAsync(payload.AsMemory(total, lenght - total), token);

                        if (readPayload == 0)
                            break;

                        total += readPayload;
                    }

                    var record = _frameParser.Parse(payload);
                    _signalAggregator.ProcessRecord(record);
                } 
                catch {

                    break;
                }
            }

            IsConnected = false;
            OnConnectionStateChanged?.Invoke();
        }

    }
}
