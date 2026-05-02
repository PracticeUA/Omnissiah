using FluentAssertions;
using OmnissiahWpfApp.Services;
using Xunit;
using System.Net.Sockets;

namespace OmnissiahWpfApp.Tests.Services;

public sealed class TcpServerServiceTests : IAsyncDisposable {

    private readonly TcpServerService _sut = new();

    [Fact]
    public void IsRunning_BeforeStart_IsFalse() {

        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_SetsIsRunningTrue() {

        await _sut.StartAsync(TestPort());

        _sut.IsRunning.Should().BeTrue();

        await _sut.StopAsync();
    }

    [Fact]
    public async Task StopAsync_SetsIsRunningFalse() {

        await _sut.StartAsync(TestPort());
        await _sut.StopAsync();

        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_IsIdempotent() {

        var port = TestPort();
        await _sut.StartAsync(port);
        await _sut.StartAsync(port); // second call should not throw

        _sut.IsRunning.Should().BeTrue();

        await _sut.StopAsync();
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_IsIdempotent() {

        await _sut.StopAsync(); // should not throw

        _sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_RaisesOnConnectionStateChanged() {

        var raised = false;
        _sut.OnConnectionStateChanged += () => raised = true;

        await _sut.StartAsync(TestPort());

        raised.Should().BeTrue();

        await _sut.StopAsync();
    }

    [Fact]
    public async Task StopAsync_RaisesOnConnectionStateChanged() {

        await _sut.StartAsync(TestPort());

        var raised = false;
        _sut.OnConnectionStateChanged += () => raised = true;

        await _sut.StopAsync();

        raised.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_AcceptsClientConnection() {

        var port = TestPort();
        await _sut.StartAsync(port);

        using var client = new TcpClient();
        var connectTask = client.ConnectAsync("127.0.0.1", port);
        await connectTask.WaitAsync(TimeSpan.FromSeconds(3));

        client.Connected.Should().BeTrue();

        await _sut.StopAsync();
    }

    [Fact]
    public async Task StartAsync_SendsDataToConnectedClient() {

        var port = TestPort();
        await _sut.StartAsync(port);

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", port);

        var stream = client.GetStream();
        var header = new byte[2];
        var readTask = stream.ReadAsync(header).AsTask();

        await readTask.WaitAsync(TimeSpan.FromSeconds(3));

        var payloadLength = header[0] | (header[1] << 8);
        payloadLength.Should().Be(28, because: "server always sends 28-byte payload");

        await _sut.StopAsync();
    }

    public async ValueTask DisposeAsync() {

        if (_sut.IsRunning)
            await _sut.StopAsync();
    }

    private static int TestPort() {

        var rng = new Random();
        return rng.Next(49200, 49900);
    }
}
