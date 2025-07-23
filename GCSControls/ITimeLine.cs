using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCSControls
{
    public interface ITimeLine
    {
        DateTime StartTime { get; set; }
        DateTime EndTime { get; set; }
        TimeSpan Interval => EndTime - StartTime;
    }

    public class TimeLine : ITimeLine, INotifyPropertyChanged
    {
        private DateTime _startTime;
        private DateTime _endTime;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged(nameof(StartTime));
                    OnPropertyChanged(nameof(Interval));
                }
            }
        }
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged(nameof(EndTime));
                    OnPropertyChanged(nameof(Interval));
                }
            }
        }
        public TimeSpan Interval => EndTime - StartTime;

        public TimeLine() {
            StartTime = DateTime.Now;
            EndTime = StartTime.AddHours(1);
        }
        public TimeLine(DateTime startTime, DateTime endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public void Shift(TimeSpan delta)
        {
            StartTime = StartTime.Add(delta);
            EndTime = EndTime.Add(delta);
        }

        public void Zoom(DateTime center, double factor, double ratio)
        {
            var newIntervalTicks = (long)(Interval.Ticks * factor);

            if (newIntervalTicks < TimeSpan.FromSeconds(5).Ticks ||
                newIntervalTicks > TimeSpan.FromDays(7).Ticks)
                return;

            var newInterval = TimeSpan.FromTicks(newIntervalTicks);

            StartTime = center - TimeSpan.FromTicks((long)(newInterval.Ticks * ratio));
            EndTime = StartTime + newInterval;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
