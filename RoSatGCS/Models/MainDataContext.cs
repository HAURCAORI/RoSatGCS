using MessagePack;
using RoSatGCS.ViewModels;
using RoSatGCS.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace RoSatGCS.Models
{
    public class MainDataContext
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly string PathCacheFuncFile = "Cache/func_file_list";
        private static readonly string PathCacheCommandFile = "Cache/command_list";

        // Pages
        private Page? pageArchive;
        private Page? pageCommand;
        private Page? pageGroundTrack;
        private Page? pageScheduler;
        private Page? pageFileShare;

        public Page PageArchive { get { if (pageArchive == null) pageArchive = new PageArchive(); return pageArchive; } }
        public Page PageCommand { get { if (pageCommand == null) pageCommand = new PageCommand(); return pageCommand; } }
        public Page PageGroundTrack { get { if (pageGroundTrack == null) pageGroundTrack = new PageGroundTrack(); return pageGroundTrack; } }
        public Page PageScheduler { get { if (pageScheduler == null) pageScheduler = new PageScheduler(); return pageScheduler; } }
        public Page PageFileShare { get { if (pageFileShare == null) pageFileShare = new PageFileShare(); return pageFileShare; } }

        public PageArchiveViewModel? GetPageArchiveViewModel
        { get => pageArchive?.DataContext as PageArchiveViewModel; }
        public PageCommandViewModel? GetPageCommandViewModel
        { get => pageCommand?.DataContext as PageCommandViewModel; }
        public PageGroundTrackViewModel? GetPageGroundTrackViewModel
        { get => pageGroundTrack?.DataContext as PageGroundTrackViewModel; }
        public PageSchedulerViewModel? GetPageSchedulerViewModel
        { get => pageScheduler?.DataContext as PageSchedulerViewModel; }
        public PageFileShareViewModel? GetPageFileShareViewModel
        { get => pageFileShare?.DataContext as PageFileShareViewModel; }


        // Satellite Function File
        private ObservableCollection<SatelliteFunctionFileModel> _satFuncFile = [];
        public ReadOnlyObservableCollection<SatelliteFunctionFileModel> SatFuncFile { get; private set; }

        private ListCollectionView _satFuncFileView;
        public ListCollectionView SatelliteFunctionFileView { get => _satFuncFileView; }

        // Satellite Function Type
        private readonly ObservableCollection<SatelliteFunctionTypeModel> _satFuncType = [];
        public ObservableCollection<SatelliteFunctionTypeModel> SatelliteFunctionTypes { get => _satFuncType; }

        private readonly ListCollectionView _satFuncTypeView;
        public ListCollectionView SatelliteFunctionTypesView { get => _satFuncTypeView; }


        // Satellite Method
        private readonly ObservableCollection<SatelliteMethodModel> _satMethod = [];
        public ObservableCollection<SatelliteMethodModel> SatelliteMethod { get => _satMethod; }

        private readonly ListCollectionView _satMethodView;
        public ListCollectionView SatelliteMethodView { get => _satMethodView; }

        // Satelltie Command
        private ObservableCollection<SatelliteCommandGroupModel> _satCommandGroup = [];
        public ReadOnlyObservableCollection<SatelliteCommandGroupModel> SatelliteCommandGroup { get; private set; }

        private ListCollectionView _satCommandGroupView;
        public ListCollectionView SatelliteCommandGroupView { get => _satCommandGroupView; }

        // Satellite Schedule Command
        private ObservableCollection<SatelliteCommandModel> _satScheduleCommand = [];
        public ObservableCollection<SatelliteCommandModel> SatelliteScheduleCommand { get => _satScheduleCommand; }

        // Plot Data Container
        private PlotDataContainer _plotDataContainer = new PlotDataContainer();
        public PlotDataContainer PlotDataContainer { get => _plotDataContainer; }

        // Beacon Data Container
        private ObservableCollection<BeaconModel> _beaconModelCollection = [];
        public ObservableCollection<BeaconModel> BeaconModelCollection { get => _beaconModelCollection; }
        private ListCollectionView _beaconModelView;
        public ListCollectionView BeaconModelView { get => _beaconModelView; }

        private MainDataContext()
        {
            SatFuncFile = new ReadOnlyObservableCollection<SatelliteFunctionFileModel>(_satFuncFile);
            SatelliteCommandGroup = new ReadOnlyObservableCollection<SatelliteCommandGroupModel>(_satCommandGroup);

            _satFuncFileView = new ListCollectionView(SatFuncFile);
            _satFuncTypeView = new ListCollectionView(_satFuncType);
            _satMethodView = new ListCollectionView(_satMethod);
            _satCommandGroupView = new ListCollectionView(SatelliteCommandGroup);
            _beaconModelView = new ListCollectionView(BeaconModelCollection);

            _satFuncFileView.SortDescriptions.Add(new SortDescription(nameof(SatelliteFunctionFileModel.Name), ListSortDirection.Ascending));
            _beaconModelView.SortDescriptions.Add(new SortDescription(nameof(BeaconModel.Timestamp), ListSortDirection.Descending));
        }

        private static Lazy<MainDataContext> _instance = new(() => new MainDataContext());
        public static MainDataContext Instance { get { return _instance.Value; } }


        public void Load()
        {
            // Load Satellite Function
            if (File.Exists(PathCacheFuncFile))
            {
                using (var fileStream = File.OpenRead(PathCacheFuncFile))
                {
                    try
                    {
                        _satFuncFile = MessagePackSerializer.Deserialize<ObservableCollection<SatelliteFunctionFileModel>>(fileStream);
                        SatFuncFile = new ReadOnlyObservableCollection<SatelliteFunctionFileModel>(_satFuncFile);
                        _satFuncFileView = new ListCollectionView(_satFuncFile);
                        _satFuncFileView.SortDescriptions.Add(new SortDescription(nameof(SatelliteFunctionFileModel.Name), ListSortDirection.Ascending));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex);
                    }
                }
            }

            if (File.Exists(PathCacheCommandFile))
            {
                using (var fileStream = File.OpenRead(PathCacheCommandFile))
                {
                    try
                    {
                        var temp = MessagePackSerializer.Deserialize<ObservableCollection<SatelliteCommandGroupModel>>(fileStream);
                        foreach (var item in temp)
                        {
                            item.Init();
                            _satCommandGroup.Add(new SatelliteCommandGroupModel(item));
                        }

                        SatelliteCommandGroup = new ReadOnlyObservableCollection<SatelliteCommandGroupModel>(_satCommandGroup);
                        _satCommandGroupView = new ListCollectionView(_satCommandGroup);

                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex);
                    }
                }
            }
        }

        public void Save()
        {
            if (!Directory.Exists("Cache"))
            {
                Directory.CreateDirectory("Cache");
            }

            // Save Satellite Function File
            using (var fileStream = File.OpenWrite(PathCacheFuncFile))
            {
                try
                {
                    MessagePackSerializer.Serialize(fileStream, _satFuncFile);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
            }
            // Save Satellite Command File
            using (var fileStream = File.OpenWrite(PathCacheCommandFile))
            {
                try
                {
                    MessagePackSerializer.Serialize(fileStream, _satCommandGroup);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
            }
        }

        public void LoadBeacon(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!File.Exists(path)) return;
            using (var fileStream = File.OpenRead(path))
            {
                try
                {
                    _beaconModelCollection = MessagePackSerializer.Deserialize<ObservableCollection<BeaconModel>>(fileStream);
                    _beaconModelView = new ListCollectionView(_beaconModelCollection);
                    _beaconModelView.SortDescriptions.Add(new SortDescription(nameof(BeaconModel.Timestamp), ListSortDirection.Ascending));
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
            }
        }

        public void SaveBeacon(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            using (var fileStream = File.OpenWrite(path))
            {
                try
                {
                    MessagePackSerializer.Serialize(fileStream, _beaconModelCollection);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
            }
        }



        // Add Satellite Function File
        public void AddSatelliteFunctionFile(SatelliteFunctionFileModel file)
        {
            if (file == null) return;
            if (!_satFuncFile.Contains(file))
            {
                _satFuncFile.Add(file);
                //_satFuncFileView.Refresh();
            }
        }

        // Remove Satellite Function File
        public void RemoveSatelliteFunctionFile(SatelliteFunctionFileModel file)
        {
            if (file == null) return;
            if (_satFuncFile.Contains(file))
            {
                _satFuncFile.Remove(file);
                //_satFuncFileView.Refresh();
            }
        }

        // Add Satellite Command Group
        public void AddSatelliteCommandGroup(SatelliteCommandGroupModel group)
        {
            if (group == null) return;
            if (!_satCommandGroup.Contains(group))
            {
                _satCommandGroup.Add(group);
                //_satCommandGroupView.Refresh();
            }
        }
        // Remove Satellite Command Group
        public void RemoveSatelliteCommandGroup(SatelliteCommandGroupModel group)
        {
            if (group == null) return;
            if (_satCommandGroup.Contains(group))
            {
                _satCommandGroup.Remove(group);
                //_satCommandGroupView.Refresh();
            }
        }
        // Add Satellite Command to Schedule
        public void AddSatelliteCommandToSchedule(SatelliteCommandModel command)
        {
            if (command == null) return;
            _satScheduleCommand.Add(command);
        }

        // Add Beacon Model
        public void AddBeaconModel(BeaconModel beacon)
        {
            if (beacon == null) return;
            _beaconModelCollection.Add(beacon);
        }

        // Remove Beacon Model
        public void RemoveBeaconModel(BeaconModel beacon)
        {
            if (beacon == null) return;
            _beaconModelCollection.Remove(beacon);
        }
    }
}
