using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmnissiahWpfApp.Models
{
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
