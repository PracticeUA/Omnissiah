using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Models
{
    /// <summary>
    /// Represents a single parsed signal frame received from the TCP stream.
    /// </summary>
    /// <remarks>
    /// Produced by <see cref="FrameParser.Parse"/> and consumed by
    /// <see cref="SignalAggregatorService.ProcessRecord"/>.
    /// All values are in their raw units as transmitted — frequency in Hz,
    /// bandwidth in Hz — and are converted to display units (MHz, kHz)
    /// during aggregation in <see cref="SignalAggregatorService"/>.
    /// </remarks>
    /// <param name="Timestamp">UTC time when the signal was captured.</param>
    /// <param name="FrequencyHz">Frequency of the signal, in Hz.</param>
    /// <param name="BandwidthHz">Bandwidth of the signal, in Hz.</param>
    /// <param name="SnrDb">Signal-to-noise ratio, in dB.</param>
    public sealed record SignalRecordModel(
        DateTime Timestamp,
        ulong FrequencyHz,
        uint BandwidthHz,
        double SnrDb
        );
}
