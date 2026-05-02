using OmnissiahWpfApp.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Helpers
{
    public sealed class FrameParser {

        public SignalRecordModel Parse(byte[] buffer) {

            var timastamp = BinaryPrimitives.ReadUInt64LittleEndian(buffer[0..8]);
            var frequency = BinaryPrimitives.ReadUInt64LittleEndian(buffer[8..16]);
            var bandwidth = BinaryPrimitives.ReadUInt32LittleEndian(buffer[16..20]);
            var snr = BitConverter.ToDouble(buffer, 20);

            return new SignalRecordModel(
                DateTimeOffset.FromUnixTimeSeconds((long)timastamp).DateTime,
                frequency,
                bandwidth,
                snr
            );
        }
    }
}
