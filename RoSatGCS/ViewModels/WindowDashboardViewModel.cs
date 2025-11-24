using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Media.PlayTo;
using WpfHexaEditor.Core.MethodExtention;

namespace RoSatGCS.ViewModels
{
    public class WindowDashboardViewModel : ViewModelBase 
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public MainDataContext MainDataContext => MainDataContext.Instance;

        private WeakReference<MainWindowViewModel>? _parent;

        public MainWindowViewModel? Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out var target))
                {
                    return target;
                }
                return null;
            }
        }

        private readonly HashSet<string> loadedBeacons = new HashSet<string>();
        private readonly BeaconModel _emptyBeaconModel;

        public PlotDataContainer PlotData { get => MainDataContext.Instance.PlotDataContainer; }

        DispatcherTimer timer = new DispatcherTimer();

        public enum PlotDataName
        {
            Px, Py, Pz,
            Vx, Vy, Vz,
            Yaw, Pitch, Roll,
            Wx, Wy, Wz,
            Voltage, Current, Temperature,
        }

        public string[] _plotDataTitle =
        {
            "GNSS Position X", "GNSS Position Y", "GNSS Position Z",
            "GNSS Velocity X", "GNSS Velocity Y", "GNSS Velocity Z",
            "Attitude Yaw", "Attitude Pitch", "Attitude Roll",
            "Angular Velocity X", "Angular Velocity Y", "Angular Velocity Z",
            "EPS Voltage", "EPS Current", "EPS Temperature",
        };

        public string[] PlotDataTitle { get => _plotDataTitle; }




        #region Properties
        private bool _topMost = false;
        public bool TopMost { get => _topMost; set => SetProperty(ref _topMost, value); }

        private string _sourcePath = "";
        public string SourcePath { get => _sourcePath; set => SetProperty(ref _sourcePath, value); }

        public BeaconModel FirstBeaconModel
        {
            get {
                if (MainDataContext.Instance.BeaconModelView.Count > 0)
                {
                    var obj = MainDataContext.Instance.BeaconModelView.GetItemAt(MainDataContext.Instance.BeaconModelView.Count - 1);
                    if (obj is BeaconModel beacon)
                    {
                        return beacon;
                    }
                }
                return _emptyBeaconModel;
            }
        }

        public BeaconModel LatestBeaconModel
        {
            get {
                if (MainDataContext.Instance.BeaconModelView.Count > 0)
                {
                    var obj = MainDataContext.Instance.BeaconModelView.GetItemAt(0);
                    if (obj is BeaconModel beacon)
                    {
                        return beacon;
                    }
                }
                return new BeaconModel();
            }
        }

        public double DurationSeconds
        {
            get
            {
                var start = FirstBeaconModel.DateTime;
                var end = LatestBeaconModel.DateTime;
                var seconds = (end - start).TotalSeconds;
                return Math.Max(1, seconds);
            }
        }

        private double _selectedOffsetSeconds = 0;
        public double SelectedOffsetSeconds
        {
            get => _selectedOffsetSeconds;
            set
            {
                if (Math.Abs(_selectedOffsetSeconds - value) > double.Epsilon)
                {
                    if (SetProperty(ref _selectedOffsetSeconds, value))
                    {
                        OnPropertyChanged(nameof(SliderDateTime));
                        UpdateCurrentBeaconModel();
                    }
                }
            }
        }

        public DateTime SliderDateTime
        {
            get
            {
                var startTime = FirstBeaconModel.DateTime;
                var targetTime = startTime.AddSeconds(SelectedOffsetSeconds);
                return targetTime;
            }
        }

        private BeaconModel _currentBeaconModel;
        public BeaconModel CurrentBeaconModel
        {
            get => _currentBeaconModel;
            set => SetProperty(ref _currentBeaconModel, value);
        }

        private DateTime _startLimit;
        public DateTime StartLimit
        {
            get => _startLimit;
            set => SetProperty(ref _startLimit, value);
        }
        private bool _isStartLimit;
        public bool IsStartLimit
        {
            get => _isStartLimit;
            set {
                if (SetProperty(ref _isStartLimit, value))
                {
                    if (_isStartLimit)
                    {
                        foreach (var loaded in loadedBeacons)
                        {
                            var beacon = MainDataContext.Instance.BeaconModelCollection.FirstOrDefault(b => b.Filename == loaded);
                            if (beacon != null && beacon.DateTime < StartLimit)
                            {
                                MainDataContext.Instance.RemoveBeaconModel(beacon);
                                loadedBeacons.Remove(loaded);
                            }
                        }
                    }

                    PlotData.Clear();
                    OnPropertyChanged(nameof(PlotData));
                    foreach (var i in MainDataContext.Instance.BeaconModelCollection)
                    {
                        i.PlotReady = false;
                    }


                    Dispatcher.CurrentDispatcher.InvokeAsync(async () =>
                    {
                        await Task.Delay(100);

                        OnRefresh();
                        OnPropertyChanged(nameof(PlotData));
                    }).Wait();
                }
            }
        }
        public bool IsRunning { get => timer.IsEnabled; }

        public Func<DateTime, string> Formatter { get; set; } = date => date.ToString("HH:mm:ss");

        #endregion

        #region Commands
        public RelayCommand BroswePath => new RelayCommand(OnBrowsePath);
        public RelayCommand Refresh => new RelayCommand(OnRefresh);
        public RelayCommand Run => new RelayCommand(() => {
            if(timer.IsEnabled)
                timer.Stop();
            else
                timer.Start();
            OnPropertyChanged(nameof(IsRunning));
        });
        #endregion

        public WindowDashboardViewModel(MainWindowViewModel parent)
        {
            _parent = new WeakReference<MainWindowViewModel>(parent);
            _emptyBeaconModel = new BeaconModel();
            _currentBeaconModel = _emptyBeaconModel;
            _startLimit = DateTime.Now;

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (object? sender, EventArgs e) => { OnRefresh(); };
            timer.Start();
        }

        public void OnBrowsePath()
        {
            var dialogue = new OpenFolderDialog
            {
                Title = "Select Source Path",
                InitialDirectory = SourcePath,
            };

            if (dialogue.ShowDialog() == false) return;

            SourcePath = dialogue.FolderName;

            OnLoadCache();
            OnDirectoryCheck();
        }

        public void OnLoadCache()
        {
            string cacheFilePath = Path.Combine(SourcePath, "cache");

            if (File.Exists(cacheFilePath) == false) return;

            MainDataContext.Instance.LoadBeacon(cacheFilePath);

            foreach(var beacon in MainDataContext.Instance.BeaconModelCollection)
            {
                loadedBeacons.Add(beacon.Filename);
            }

            OnPropertyChanged(nameof(FirstBeaconModel));
            OnPropertyChanged(nameof(LatestBeaconModel));
            OnPropertyChanged(nameof(DurationSeconds));
            UpdateCurrentBeaconModel();
        }

        public void OnRefresh()
        {
            OnDirectoryCheck();
            OnPlotCheck();
        }

        public void OnDirectoryCheck()
        {
            if(string.IsNullOrEmpty(SourcePath)) return;

            var beaconFiles = Directory.GetFiles(SourcePath, "beacon_*.json");
            foreach (var file in beaconFiles)
            {
                if(loadedBeacons.Contains(Path.GetFileName(file))) continue;

                try
                {
                    var beacon = new BeaconModel();
                    beacon.Initialize(file);

                    if(IsStartLimit && beacon.DateTime < StartLimit)
                    {
                        continue;
                    }

                    MainDataContext.Instance.AddBeaconModel(beacon);
                    loadedBeacons.Add(beacon.Filename);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to load beacon file: " + file);
                }
            }

            OnPropertyChanged(nameof(FirstBeaconModel));
            OnPropertyChanged(nameof(LatestBeaconModel));
            OnPropertyChanged(nameof(DurationSeconds));
            UpdateCurrentBeaconModel();
        }
        public void OnPlotCheck()
        {
            var startTime = FirstBeaconModel.DateTime;
            var targetTime = startTime.AddSeconds(SelectedOffsetSeconds);

            foreach (var m in MainDataContext.Instance.BeaconModelView)
            {
                if (m is BeaconModel model)
                {
                    if (model.PlotReady) continue;
                    if (model.DateTime > targetTime) continue;
                    if (IsStartLimit && model.DateTime < StartLimit) continue;
                    AddSpecificPlotData(model, PlotDataName.Px);
                    AddSpecificPlotData(model, PlotDataName.Py);
                    AddSpecificPlotData(model, PlotDataName.Pz);
                    AddSpecificPlotData(model, PlotDataName.Vx);
                    AddSpecificPlotData(model, PlotDataName.Vy);
                    AddSpecificPlotData(model, PlotDataName.Vz);
                    AddSpecificPlotData(model, PlotDataName.Yaw);
                    AddSpecificPlotData(model, PlotDataName.Pitch);
                    AddSpecificPlotData(model, PlotDataName.Roll);
                    AddSpecificPlotData(model, PlotDataName.Wx);
                    AddSpecificPlotData(model, PlotDataName.Wy);
                    AddSpecificPlotData(model, PlotDataName.Wz);
                    AddSpecificPlotData(model, PlotDataName.Voltage);
                    AddSpecificPlotData(model, PlotDataName.Current);
                    AddSpecificPlotData(model, PlotDataName.Temperature);
                    model.PlotReady = true;
                }
            }
        }

        public void AddSpecificPlotData(BeaconModel model, PlotDataName name)
        {
            switch (name)
            {
                case PlotDataName.Px:
                    byte[] value = BitConverter.GetBytes((double)model.Structure.gnss_data.px);
                    PlotData.Add((ushort) name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Py:
                    value = BitConverter.GetBytes((double)model.Structure.gnss_data.py);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Pz:
                    value = BitConverter.GetBytes((double)model.Structure.gnss_data.pz);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Vx:
                    value = BitConverter.GetBytes((double)model.Structure.gnss_data.vx);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Vy:
                    value = BitConverter.GetBytes((double)model.Structure.gnss_data.vy);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Vz:
                    value = BitConverter.GetBytes((double)model.Structure.gnss_data.vz);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Yaw:
                    value = BitConverter.GetBytes((double)model.Structure.imu_data.yaw);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Pitch:
                    value = BitConverter.GetBytes((double)model.Structure.imu_data.pitch);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Roll:
                    value = BitConverter.GetBytes((double)model.Structure.imu_data.roll);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Wx:
                    value = BitConverter.GetBytes((double)model.Structure.adcs_data.angle_rate.wx);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Wy:
                    value = BitConverter.GetBytes((double)model.Structure.adcs_data.angle_rate.wy);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Wz:
                    value = BitConverter.GetBytes((double)model.Structure.adcs_data.angle_rate.wz);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Voltage:
                    value = BitConverter.GetBytes((double)model.Structure.eps_data.voltage);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Current:
                    value = BitConverter.GetBytes((double)model.Structure.eps_data.current);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
                case PlotDataName.Temperature:
                    value = BitConverter.GetBytes((double)model.Structure.eps_data.temperature);
                    PlotData.Add((ushort)name, CollectionsMarshal.AsSpan([new PlotData { DateTime = model.DateTime, PlotDataType = Utils.Query.DataType.Double, Data = value }]));
                    break;
            }
        }

        private void UpdateCurrentBeaconModel()
        {
            if (IsRunning)
            {
                SelectedOffsetSeconds = DurationSeconds;
            }


            var startTime = FirstBeaconModel.DateTime;
            var targetTime = startTime.AddSeconds(SelectedOffsetSeconds);

            var collection = MainDataContext.Instance.BeaconModelView;
            if (collection == null || collection.Count == 0)
            {
                CurrentBeaconModel = _emptyBeaconModel;
                return;
            }


            // Collection is sorted descending by DateTime
            foreach (var beacon in collection)
            {
                if (beacon is BeaconModel bm)
                {
                    if (bm.DateTime <= targetTime)
                    {
                        if (CurrentBeaconModel != bm)
                        {
                            CurrentBeaconModel = bm;
                        }
                        return;
                    }
                }
            }
        }
    }
}
