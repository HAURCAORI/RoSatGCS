using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RoSatGCS.Utils.Timer
{
    public class TimeUpdateManager
    {
        private static readonly Lazy<TimeUpdateManager> _instance = new(() => new TimeUpdateManager());

        public static TimeUpdateManager Instance => _instance.Value;

        private readonly DispatcherTimer _timer;
        private readonly List<Action> _subscribers = new();
        private readonly object _lock = new object();

        private TimeUpdateManager()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _timer.Tick += (s, e) => NotifySubscribers();
            _timer.Start();
        }

        public void Subscribe(Action updateCallback)
        {
            lock (_lock)
            {
                if (!_subscribers.Contains(updateCallback))
                {
                    _subscribers.Add(updateCallback);
                }
            }
        }

        public void Unsubscribe(Action updateCallback)
        {
            lock (_lock)
            {
                if (_subscribers.Contains(updateCallback))
                {
                    _subscribers.Remove(updateCallback);
                }
            }

        }

        private void NotifySubscribers()
        {
            lock (_lock)
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Invoke();
                }
            }
        }

        public static string FormatElapsedTime(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 60)
            {
                int seconds = (int)elapsed.TotalSeconds;
                return $"{seconds} second{(seconds == 1 ? "" : "s")} ago";
            }
            if (elapsed.TotalMinutes < 60)
            {
                int minutes = (int)elapsed.TotalMinutes;
                return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
            }
            if (elapsed.TotalHours < 24)
            {
                int hours = (int)elapsed.TotalHours;
                return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
            }
            int days = (int)elapsed.TotalDays;
            return $"{days} day{(days == 1 ? "" : "s")} ago";
        }
    }
}
