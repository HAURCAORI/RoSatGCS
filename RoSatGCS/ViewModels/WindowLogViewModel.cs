using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Utils.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RoSatGCS.ViewModels
{
    internal class WindowLogViewModel
    {
        public bool TopMost { get; set; } = false;
        public bool AutoScroll { get; set; } = true;
        public RelayCommand ClearLogCommand { get; set; }
        public RelayCommand SaveLog { get; set; }
        public RelayCommand<LogEntry> CopyLogCommand { get; }
        public RelayCommand<LogEntry> ShowLogCommand { get; }
        public Logger Logger { get; } = Logger.Instance;


        public WindowLogViewModel()
        {
            ClearLogCommand = new RelayCommand(() => { Logger.Clear(); });

            SaveLog = new RelayCommand(() =>
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "log"; // Default file name
                dlg.DefaultExt = ".txt"; // Default file extension
                dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();
                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string filename = dlg.FileName;
                    try
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
                        {
                            foreach (var log in Logger.Logs)
                            {
                                file.WriteLine(log.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to save log file.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });

            CopyLogCommand = new RelayCommand<LogEntry>(line =>
            {
                if(line != null)
                    Clipboard.SetText(line.ToString());
            });

            ShowLogCommand = new RelayCommand<LogEntry>(line =>
            {
                if (line != null)
                    Application.Current.Dispatcher.Invoke(() => 
                    MessageBox.Show(line.ToString(), "Log Entry", MessageBoxButton.OK, MessageBoxImage.None));
            });

        }
    }
}
