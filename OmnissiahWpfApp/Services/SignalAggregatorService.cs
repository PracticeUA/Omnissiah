using OmnissiahWpfApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Services 
{

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
