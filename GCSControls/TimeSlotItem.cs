using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCSControls
{
    public enum TimeSlotPriority
    {
        High,
        Mid,
        Low
    }

    public class TimeSlotItem : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private DateTime _startTime;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged(nameof(StartTime));
                }
            }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged(nameof(EndTime));
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        private TimeSlotPriority _priority;
        public TimeSlotPriority Priority
        {
            get => _priority;
            set
            {
                if (value != _priority)
                {
                    _priority = value;
                    OnPropertyChanged(nameof(Priority));
                }
            }
        }

        public TimeSlotItem(string name, DateTime startTime, DateTime endTime, TimeSlotPriority priority = TimeSlotPriority.Mid)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            StartTime = startTime;
            EndTime = endTime;
            Priority = priority;
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
