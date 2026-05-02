using OmnissiahWpfApp.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Helpers
{
    /// <summary>
    /// Parses a raw binary frame into a <see cref="SignalRecordModel"/>.
    /// </summary>
    /// <remarks>
    /// Expected frame layout (28 bytes, all fields little-endian):
    /// <code>
    /// [0..8)   uint64  Unix timestamp, seconds since epoch (UTC)
    /// [8..16)  uint64  Frequency, Hz
    /// [16..20) uint32  Bandwidth, Hz
    /// [20..28) float64 SNR, dB
    /// </code>
    /// </remarks>
    public sealed class FrameParser {

        public SignalRecordModel Parse(byte[] buffer) {

            var timastamp = BinaryPrimitives.ReadUInt64LittleEndian(buffer[0..8]);
            var frequency = BinaryPrimitives.ReadUInt64LittleEndian(buffer[8..16]);
            var bandwidth = BinaryPrimitives.ReadUInt32LittleEndian(buffer[16..20]);
            var snr = BitConverter.ToDouble(buffer, 20);

            return new SignalRecordModel(
                DateTimeOffset.FromUnixTimeSeconds((long)timastamp).UtcDateTime,
                frequency,
                bandwidth,
                snr
            );
        }
    }
}
