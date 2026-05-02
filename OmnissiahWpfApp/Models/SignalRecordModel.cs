using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Models
{
    public sealed record SignalRecordModel(
        DateTime Timestamp,
        ulong FrequencyHz,
        uint BandwidthHz,
        double SnrDb
        );
}
