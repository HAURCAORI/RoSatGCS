using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RoSatGCS.Utils.Logger
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level.ToString().ToUpper()}: {Message}";
        }
    }

    public class Logger
    {
        ObservableCollection<LogEntry> logs = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> Logs { get { return logs; } }

        // Log Options
        public int MaxLogEntries { get; set; } = 1000;

        // Singleton instance
        private static readonly Lazy<Logger> instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance { get { return instance.Value; } }
        public Logger() { }

        private static void Log(string message, LogLevel level)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Instance.logs.Count >= Instance.MaxLogEntries)
                {
                    Instance.logs.RemoveAt(0);
                }
                Instance.logs.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Message = message
                });
            }); 
        }

        public static void LogInfo(string message)
        {
            Log($"{message}", LogLevel.Info);
        }
        public static void LogWarning(string message)
        {
            Log($"{message}", LogLevel.Warning);
        }
        public static void LogError(string message)
        {
            Log($"{message}", LogLevel.Error);
        }

        public static void Clear()
        {
            Instance.logs.Clear();
        }
    }
}
