using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Models
{
    /// <summary>
    /// Represents an aggregated signal record displayed as a single row in the data grid.
    /// </summary>
    /// <remarks>
    /// Created by <see cref="SignalAggregatorService"/> when an incoming signal falls outside
    /// the frequency band of the current record. Consecutive signals within the same band
    /// increment <see cref="Count"/> rather than producing a new record.
    ///
    /// Immutable identity fields (<see cref="Timestamp"/>, <see cref="FrequencyMHz"/>,
    /// <see cref="BandwidthKHz"/>, <see cref="SnrDb"/>) are set once at creation via init-only setters.
    ///
    /// <see cref="IsNew"/> is set to <c>true</c> on creation and flipped to <c>false</c>
    /// after 1.4 seconds by <see cref="MainWindowViewModel"/>, driving the blue flash
    /// animation defined in the row style DataTrigger.
    /// </remarks>
    public sealed class SignalAggregatedRecordModel : ObservableObject {

        public DateTime Timestamp { get; init; }
        public double FrequencyMHz { get; init; }
        public double BandwidthKHz { get; init; }
        public double SnrDb { get; init; }

        private int _count;
        public int Count 
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        private bool _isNew = true;
        public bool IsNew
        {
            get => _isNew;
            set => SetProperty(ref _isNew, value);
        }
    }
}
