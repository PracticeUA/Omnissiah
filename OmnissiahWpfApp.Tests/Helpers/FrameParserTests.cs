using FluentAssertions;
using OmnissiahWpfApp.Helpers;
using Xunit;
using System.Buffers.Binary;

namespace OmnissiahWpfApp.Tests.Helpers;

public sealed class FrameParserTests {

    private readonly FrameParser _parser = new();

    private static byte[] BuildBuffer(ulong unixSeconds, ulong frequencyHz, uint bandwidthHz, double snrDb) {

        var buffer = new byte[28];
        BitConverter.GetBytes(unixSeconds).CopyTo(buffer, 0);
        BitConverter.GetBytes(frequencyHz).CopyTo(buffer, 8);
        BitConverter.GetBytes(bandwidthHz).CopyTo(buffer, 16);
        BitConverter.GetBytes(snrDb).CopyTo(buffer, 20);
        return buffer;
    }

    [Fact]
    public void Parse_ValidBuffer_ReturnsCorrectTimestamp() {

        var now = DateTimeOffset.UtcNow;
        var unixSeconds = (ulong)now.ToUnixTimeSeconds();
        var buffer = BuildBuffer(unixSeconds, 100_000_000UL, 25_000U, 10.0);

        var record = _parser.Parse(buffer);

        var expected = DateTimeOffset.FromUnixTimeSeconds((long)unixSeconds).DateTime;
        record.Timestamp.Should().Be(expected);
    }

    [Fact]
    public void Parse_ValidBuffer_ReturnsCorrectFrequency() {

        const ulong expectedFreq = 433_920_000UL;
        var buffer = BuildBuffer(1_000_000UL, expectedFreq, 25_000U, 5.0);

        var record = _parser.Parse(buffer);

        record.FrequencyHz.Should().Be(expectedFreq);
    }

    [Fact]
    public void Parse_ValidBuffer_ReturnsCorrectBandwidth() {

        const uint expectedBw = 125_000U;
        var buffer = BuildBuffer(1_000_000UL, 100_000_000UL, expectedBw, 5.0);

        var record = _parser.Parse(buffer);

        record.BandwidthHz.Should().Be(expectedBw);
    }

    [Fact]
    public void Parse_ValidBuffer_ReturnsCorrectSnr() {

        const double expectedSnr = 34.567;
        var buffer = BuildBuffer(1_000_000UL, 100_000_000UL, 25_000U, expectedSnr);

        var record = _parser.Parse(buffer);

        record.SnrDb.Should().BeApproximately(expectedSnr, precision: 1e-10);
    }

    [Fact]
    public void Parse_ZeroSnr_ReturnsZeroSnr() {

        var buffer = BuildBuffer(1_000_000UL, 100_000_000UL, 25_000U, 0.0);

        var record = _parser.Parse(buffer);

        record.SnrDb.Should().Be(0.0);
    }

    [Fact]
    public void Parse_MaxFrequency_ReturnsCorrectFrequency() {

        const ulong maxFreq = ulong.MaxValue;
        var buffer = BuildBuffer(1_000_000UL, maxFreq, 25_000U, 1.0);

        var record = _parser.Parse(buffer);

        record.FrequencyHz.Should().Be(maxFreq);
    }

    [Fact]
    public void Parse_LargeUnixTimestamp_ReturnsCorrectTimestamp() {

        var future = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var unixSeconds = (ulong)future.ToUnixTimeSeconds();
        var buffer = BuildBuffer(unixSeconds, 100_000_000UL, 25_000U, 1.0);

        var record = _parser.Parse(buffer);

        record.Timestamp.Should().Be(future.DateTime);
    }

    [Fact]
    public void Parse_AllKnownBandwidths_AreParsedCorrectly() {

        uint[] bandwidths = [12_500, 25_000, 50_000, 125_000];

        foreach (var bw in bandwidths) {

            var buffer = BuildBuffer(1_000_000UL, 100_000_000UL, bw, 20.0);
            var record = _parser.Parse(buffer);
            record.BandwidthHz.Should().Be(bw, because: $"bandwidth {bw} Hz should round-trip correctly");
        }
    }
}
