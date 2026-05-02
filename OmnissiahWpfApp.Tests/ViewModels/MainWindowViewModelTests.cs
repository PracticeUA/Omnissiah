using FluentAssertions;
using OmnissiahWpfApp.Services;
using Xunit;
using OmnissiahWpfApp.ViewModels;

namespace OmnissiahWpfApp.Tests.ViewModels;

/// <summary>
/// The dispatcher is only involved when SignalAggregatorService raises OnRecordCreated.
/// </summary>
public sealed class MainWindowViewModelTests {

    private static (MainWindowViewModel vm, TcpServerService server, TcpClientService client, SignalAggregatorService aggregator) 
        CreateSut() {

        var aggregator = new SignalAggregatorService();
        var server = new TcpServerService();
        var client = new TcpClientService(aggregator);
        var vm = new MainWindowViewModel(server, client, aggregator);
        return (vm, server, client, aggregator);
    }

    [Fact]
    public void ServerButtonText_WhenServerNotRunning_ReturnsStartLabel() {

        var (vm, _, _, _) = CreateSut();

        vm.ServerButtonText.Should().Be("Start server");
    }

    [Fact]
    public void ClientButtonText_WhenClientNotConnected_ReturnsStartLabel() {

        var (vm, _, _, _) = CreateSut();

        vm.ClientButtonText.Should().Be("Start client");
    }

    [Fact]
    public void Records_OnCreation_IsEmpty() {

        var (vm, _, _, _) = CreateSut();

        vm.Records.Should().BeEmpty();
    }

    [Fact]
    public void ToggleServerCommand_IsNotNull() {

        var (vm, _, _, _) = CreateSut();

        vm.ToggleServerCommand.Should().NotBeNull();
    }

    [Fact]
    public void ToggleClientCommand_IsNotNull() {

        var (vm, _, _, _) = CreateSut();

        vm.ToggleClientCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task ServerButtonText_AfterServerStarts_ReturnsStopLabel() {

        var (vm, server, _, _) = CreateSut();

        await server.StartAsync(49910);
        // give the server a tick to update and notify
        await Task.Delay(50);

        try {

            vm.ServerButtonText.Should().Be("Stop server");
        }
        finally {

            await server.StopAsync();
        }
    }

    [Fact]
    public async Task ServerButtonText_AfterServerStops_ReturnsStartLabel() {

        var (vm, server, _, _) = CreateSut();

        await server.StartAsync(49911);
        await server.StopAsync();
        await Task.Delay(50);

        vm.ServerButtonText.Should().Be("Start server");
    }

    [Fact]
    public void Records_IsObservableCollection_NotNull() {

        var (vm, _, _, _) = CreateSut();

        vm.Records.Should().NotBeNull();
    }
}
