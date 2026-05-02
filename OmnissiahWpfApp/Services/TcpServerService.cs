using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Services
{
    public sealed class TcpServerService {

        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private long _currentCenterFrequency = 100_000_000;
        private uint _currentCenterBandwidth = 25_000;

        public event Action? OnConnectionStateChanged;

        public bool IsRunning { get; private set; }

        public async Task StartAsync(int port = 4444) {

            if (IsRunning) 
                return;

            _cts = new CancellationTokenSource();

            _listener = new TcpListener(System.Net.IPAddress.Any, port);
            _listener.Start();

            IsRunning = true;
            OnConnectionStateChanged?.Invoke();

            _ = AcceptLoop(_cts.Token);

            await Task.CompletedTask;
        }

        public async Task StopAsync() {

            if (!IsRunning) 
                return;

            _cts?.Cancel();
            _listener?.Stop();
            IsRunning = false;
            OnConnectionStateChanged?.Invoke();

            await Task.CompletedTask;
        }

        private async Task AcceptLoop(CancellationToken token) {

            while (!token.IsCancellationRequested) {

                try {

                    var client = await _listener!.AcceptTcpClientAsync(token);
                    _ = HandleClient(client, token);
                } 
                catch {

                    break;
                }
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken token) {

            await using var stream = client.GetStream();

            var random = new Random();

            while (!token.IsCancellationRequested) {

                var frame = BuildFrame(random);

                try {

                    await stream.WriteAsync(frame, token);
                } 
                catch {

                    break;
                }
                await Task.Delay(100, token);
            }
        }

        private byte[] BuildFrame(Random random) {

            var payload = new byte[28];

            BitConverter.GetBytes((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()).CopyTo(payload, 0);

            long frequency;
            uint bandwidth;

            var inRangeChance = random.Next(0, 100);
            
            if (inRangeChance < 80) {

                frequency = _currentCenterFrequency + random.NextInt64(-(long)_currentCenterBandwidth / 3, (long)_currentCenterBandwidth / 3);
                bandwidth = _currentCenterBandwidth;
            } 
            else {

                var direction = random.Next(0, 2);
                if (direction == 0) {

                    _currentCenterFrequency += random.NextInt64(50_000, 150_000);

                }
                else {

                    _currentCenterFrequency -= random.NextInt64(50_000, 150_000);
                }

                frequency = _currentCenterFrequency;
                
                uint[] possibleBandwidths =
                [
                    12_500,
                    25_000,
                    50_000,
                    125_000
                ];

                _currentCenterBandwidth = possibleBandwidths[random.Next(0, possibleBandwidths.Length)];

                bandwidth = _currentCenterBandwidth;
            }

            BitConverter.GetBytes((ulong)frequency).CopyTo(payload, 8);

            BitConverter.GetBytes(bandwidth).CopyTo(payload, 16);

            BitConverter.GetBytes(random.NextDouble() * 50).CopyTo(payload, 20);

            ushort header = (ushort)payload.Length;

            var result = new byte[payload.Length + 2];

            result[0] = (byte)(header & 0xFF);
            result[1] = (byte)((header >> 8) & 0xFF);

            payload.CopyTo(result, 2);

            return result;
        }
    }
}
