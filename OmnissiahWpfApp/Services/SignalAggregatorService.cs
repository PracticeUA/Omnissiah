using OmnissiahWpfApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Services 
{
    /// <summary>
    /// Aggregates a stream of raw signal records into frequency-band buckets.
    /// </summary>
    /// <remarks>
    /// Each call to <see cref="ProcessRecord"/> either increments the count of the current
    /// aggregated record or finalizes it and starts a new one, depending on whether the
    /// incoming signal falls within the current record's frequency band.
    ///
    /// A signal is considered part of the current band if its frequency satisfies:
    /// <code>
    /// center - bandwidth/2 &lt;= FrequencyHz &lt; center + bandwidth/2
    /// </code>
    ///
    /// <see cref="OnRecordCreated"/> is raised on the calling thread (typically a background
    /// TCP receive thread) — subscribers are responsible for marshalling to the UI thread
    /// if needed.
    ///
    /// Thread safety is guaranteed by an internal lock — <see cref="ProcessRecord"/>
    /// may be called concurrently from multiple threads.
    /// </remarks>
    public sealed class SignalAggregatorService {

        private readonly object _locker = new();
        private SignalAggregatedRecordModel? _currentRecord;

        public event Action<SignalAggregatedRecordModel>? OnRecordCreated;
        
        public void ProcessRecord(SignalRecordModel record) {

            lock (_locker) {

                if (_currentRecord == null) {

                    _currentRecord = CreateAggregatedRecord(record);
                    OnRecordCreated?.Invoke(_currentRecord);

                    return;
                }

                var center = _currentRecord.FrequencyMHz * 1_000_000.0; // MHz back to Hz
                var bandwidth = _currentRecord.BandwidthKHz * 1000.0; // kHz back to Hz
                var left = center - bandwidth / 2;
                var right = center + bandwidth / 2;

                if (record.FrequencyHz >= left && record.FrequencyHz < right) 
                {
                    _currentRecord.Count++;

                    return;
                }
                
                _currentRecord = CreateAggregatedRecord(record);
                OnRecordCreated?.Invoke(_currentRecord);
            }
        }

        private static SignalAggregatedRecordModel CreateAggregatedRecord(SignalRecordModel record) {

            return new SignalAggregatedRecordModel {

                Timestamp = record.Timestamp,
                FrequencyMHz = record.FrequencyHz / 1_000_000.0, // Hz to MHz
                BandwidthKHz = record.BandwidthHz / 1000.0, // Hz to kHz
                SnrDb = record.SnrDb,
                Count = 1
            };
        }

    }
}
