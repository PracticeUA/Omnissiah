using FluentAssertions;
using OmnissiahWpfApp.Models;
using Xunit;
using OmnissiahWpfApp.Services;

namespace OmnissiahWpfApp.Tests.Services;

public sealed class SignalAggregatorServiceTests {

    private readonly SignalAggregatorService _sut = new();

    private static SignalRecordModel MakeRecord(ulong frequencyHz, uint bandwidthHz = 25_000, double snrDb = 10.0)
        => new(DateTime.UtcNow, frequencyHz, bandwidthHz, snrDb);

    [Fact]
    public void ProcessRecord_FirstRecord_RaisesOnRecordCreated() {

        SignalAggregatedRecordModel? raised = null;
        _sut.OnRecordCreated += r => raised = r;

        _sut.ProcessRecord(MakeRecord(100_000_000UL));

        raised.Should().NotBeNull();
    }

    [Fact]
    public void ProcessRecord_FirstRecord_CreatesAggregatedRecordWithCountOne() {

        SignalAggregatedRecordModel? raised = null;
        _sut.OnRecordCreated += r => raised = r;

        _sut.ProcessRecord(MakeRecord(100_000_000UL));

        raised!.Count.Should().Be(1);
    }

    [Fact]
    public void ProcessRecord_FirstRecord_ConvertsFrequencyToMHz() {

        SignalAggregatedRecordModel? raised = null;
        _sut.OnRecordCreated += r => raised = r;

        _sut.ProcessRecord(MakeRecord(433_920_000UL));

        raised!.FrequencyMHz.Should().BeApproximately(433.92, precision: 1e-6);
    }

    [Fact]
    public void ProcessRecord_FirstRecord_ConvertsBandwidthToKHz() {

        SignalAggregatedRecordModel? raised = null;
        _sut.OnRecordCreated += r => raised = r;

        _sut.ProcessRecord(MakeRecord(100_000_000UL, bandwidthHz: 25_000));

        raised!.BandwidthKHz.Should().BeApproximately(25.0, precision: 1e-6);
    }

    [Fact]
    public void ProcessRecord_FirstRecord_PreservesSnr() {

        SignalAggregatedRecordModel? raised = null;
        _sut.OnRecordCreated += r => raised = r;

        _sut.ProcessRecord(MakeRecord(100_000_000UL, snrDb: 27.3));

        raised!.SnrDb.Should().BeApproximately(27.3, precision: 1e-10);
    }

    [Fact]
    public void ProcessRecord_RecordWithinBand_IncrementsCount() {

        // center = 100 MHz, bw = 25 kHz => range [99987500, 100012500)
        const ulong center = 100_000_000UL;
        const uint bw = 25_000U;
        const ulong insideBand = 100_005_000UL; // center + 5 kHz (within range)

        SignalAggregatedRecordModel? current = null;
        _sut.OnRecordCreated += r => current = r;

        _sut.ProcessRecord(MakeRecord(center, bw));
        _sut.ProcessRecord(MakeRecord(insideBand, bw));

        current!.Count.Should().Be(2);
    }

    [Fact]
    public void ProcessRecord_RecordWithinBand_DoesNotRaiseNewEvent() {

        const ulong center = 100_000_000UL;
        const uint bw = 25_000U;
        const ulong insideBand = 100_005_000UL;

        var eventCount = 0;
        _sut.OnRecordCreated += _ => eventCount++;

        _sut.ProcessRecord(MakeRecord(center, bw));
        _sut.ProcessRecord(MakeRecord(insideBand, bw));

        eventCount.Should().Be(1);
    }

    [Fact]
    public void ProcessRecord_RecordOutsideBand_RaisesNewEvent() {

        const ulong center = 100_000_000UL;
        const uint bw = 25_000U;
        const ulong outside = 200_000_000UL;

        var eventCount = 0;
        _sut.OnRecordCreated += _ => eventCount++;

        _sut.ProcessRecord(MakeRecord(center, bw));
        _sut.ProcessRecord(MakeRecord(outside, bw));

        eventCount.Should().Be(2);
    }

    [Fact]
    public void ProcessRecord_RecordOutsideBand_ResetsCountToOne() {

        const ulong center = 100_000_000UL;
        const uint bw = 25_000U;
        const ulong outside = 200_000_000UL;

        SignalAggregatedRecordModel? latest = null;
        _sut.OnRecordCreated += r => latest = r;

        _sut.ProcessRecord(MakeRecord(center, bw));
        _sut.ProcessRecord(MakeRecord(center, bw));   // count = 2
        _sut.ProcessRecord(MakeRecord(outside, bw));  // new

        latest!.Count.Should().Be(1);
    }

    [Fact]
    public void ProcessRecord_MultipleRecordsInBand_CountsAll() {

        const ulong center = 100_000_000UL;
        const uint bw = 25_000U;
        const ulong inside = 100_003_000UL;
        const int totalRecords = 10;

        SignalAggregatedRecordModel? current = null;
        _sut.OnRecordCreated += r => current = r;

        _sut.ProcessRecord(MakeRecord(center, bw));
        for (var i = 1; i < totalRecords; i++)
            _sut.ProcessRecord(MakeRecord(inside, bw));

        current!.Count.Should().Be(totalRecords);
    }

}
