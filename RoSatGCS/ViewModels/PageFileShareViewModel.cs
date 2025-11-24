using CommunityToolkit.Mvvm.Input;
using FileListView.Interfaces;
using FileSystemModels;
using FileSystemModels.Browse;
using FileSystemModels.Events;
using FileSystemModels.Interfaces;
using Microsoft.Win32;
using RoSatGCS.Models;
using RoSatGCS.Utils.Files;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Utils.Query;
using RoSatGCS.Utils.Satellites.TLE;
using RoSatGCS.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using static RoSatGCS.Utils.Files.FileConvertHelper;

namespace RoSatGCS.ViewModels
{
    public class PageFileShareViewModel : FileTreeListControllerViewModel, IDisposable
    {
        private readonly SemaphoreSlim _SlowStuffSemaphore;
        private readonly OneTaskLimitedScheduler _OneTaskScheduler;
        private readonly CancellationTokenSource _CancelTokenSource;
        private bool _disposed = false;

        #region FW Parameters
        private ulong _satelliteId = 2;
        private byte _moduleMac = 0x33;
        private ushort _boardRevision = 0;
        private ushort _cpuType = 0;
        private ushort _subModule = 2;
        private ushort _fwType = 0;
        private ushort _fwVerMaj = 0;
        private ushort _fwVerMin = 0;
        private ushort _moduleType = 0;
        private ushort _moduleConfig = 0;

        private ulong _flags = 1024;
        public string SatId
        {
            get => _satelliteId.ToString();
            set { if (UInt64.TryParse(value, out var ret)) SetProperty(ref _satelliteId, ret); }
        }
        public string ModuleMac
        {
            get => _moduleMac.ToString();
            set { if (byte.TryParse(value, out var ret)) SetProperty(ref _moduleMac, ret); }
        }

        public string BoardRevision
        {
            get => _boardRevision.ToString();
            set { if (UInt16.TryParse(value, out var ret)) SetProperty(ref _boardRevision, ret); }
        }

        public string CpuType
        {
            get => _cpuType.ToString();
            set { if (UInt16.TryParse(value, out var ret)) SetProperty(ref _cpuType, ret); }
        }

        public string SubModule
        {
            get => _subModule.ToString();
            set { if (UInt16.TryParse(value, out var ret)) SetProperty(ref _subModule, ret); }
        }

        public string FwType
        {
            get => _fwType.ToString();
            set { if (UInt16.TryParse(value, out var ret)) SetProperty(ref _fwType, ret); }
        }

        public string FwVerMaj
        {
            get => _fwVerMaj.ToString();
            set { if (UInt16.TryParse(value, out var ret)) SetProperty(ref _fwVerMaj, ret); }
        }

        public string FwVerMin
        {
            get => _fwVerMin.ToString();
            set { if (UInt16.TryParse(value, out var ret)) SetProperty(ref _fwVerMin, ret); }
        }

        public string ModuleType
        {
            get => _moduleType.ToString();
            set { if (UInt16.TryParse(value, out var ret)) SetProperty(ref _moduleType, ret); }
        }

        public string ModuleConfig
        {
            get => _moduleConfig.ToString();
            set { if (UInt16.TryParse(value, out var ret)) SetProperty(ref _moduleConfig, ret); }
        }

        public string Flags
        {
            get => _flags.ToString();
            set { if (UInt64.TryParse(value, out var ret)) SetProperty(ref _flags, ret); }
        }



        #endregion

        #region FileDownload Parameters
        private bool _isFileDownloadStopped = true;
        public bool IsFileDownloadStopped
        {
            get => _isFileDownloadStopped;
            set => SetProperty(ref _isFileDownloadStopped, value);
        }

        private string _fileDownloadPath = string.Empty;
        public string FileDownloadPath
        {
            get => _fileDownloadPath;
            set => SetProperty(ref _fileDownloadPath, value);
        }

        private DateTime _selectedScheduleDate = DateTime.Now;
        public DateTime SelectedScheduleDate 
        {
            get => _selectedScheduleDate;
            set => SetProperty(ref _selectedScheduleDate, value);
        }

        private bool _isEnableSchedule = false;
        public bool IsEnableSchedule
        {
            get => _isEnableSchedule;
            set => SetProperty(ref _isEnableSchedule, value);
        }

        private string _savePath = string.Empty;
        public string SavePath
        {
            get => _savePath;
            set => SetProperty(ref _savePath, value);
        }

        private string _downloadStauts = "None";
        public string DownloadStatus
        {
            get => _downloadStauts;
            set => SetProperty(ref _downloadStauts, value);
        }
        
        private ObservableCollection<FileDownloadModel> _fileDownloadList = new();
        public ObservableCollection<FileDownloadModel> FileDownloadList
        {
            get => _fileDownloadList;
            set => SetProperty(ref _fileDownloadList, value);
        }

        private ObservableCollection<FileDownloadModel> _selectedFileDownloadItem = new();
        public ObservableCollection<FileDownloadModel> SelectedFileDownloadItem
        {
            get => _selectedFileDownloadItem;
            set => SetProperty(ref _selectedFileDownloadItem, value);
        }


        public RelayCommand AddFileDownloadPath { get; }
        public RelayCommand BrowseSavePath { get; }
        public RelayCommand StartDownloadCommand { get; }
        public RelayCommand StopDownloadCommand { get; }

        // Context Menu
        public RelayCommand RemoveFileDownListElement { get; }
        public RelayCommand AddFileDownListCommand { get; }
        public RelayCommand<object> UpdateFileDownloadListItmes { get; }

        public RelayCommand DownloadFileMoveUp { get; }
        public RelayCommand DownloadFileMoveDown { get; }

        #endregion

        private bool _isFileTransfering = false;

        public bool IsFileTransfering
        {
            get => _isFileTransfering;
            set {
                SetProperty(ref _isFileTransfering, value);
                FwUpdateCommand.NotifyCanExecuteChanged();
                FwUpdateBundleCommand.NotifyCanExecuteChanged();
                FileUploadCommand.NotifyCanExecuteChanged();
                FileDownload.NotifyCanExecuteChanged();
                RemoteRefreshCommand.NotifyCanExecuteChanged();
                RemoteEraseCommand.NotifyCanExecuteChanged();
                RemoteCancel.NotifyCanExecuteChanged();
                RemoteSelectedEraseCommand.NotifyCanExecuteChanged();
                RemoteSelectedDownloadCommand.NotifyCanExecuteChanged();
            }
        }

        public DateTime ListCreatedTime => File.Exists("DirList.txt") ? File.GetLastWriteTime("DirList.txt") : DateTime.MinValue;

        private ObservableCollection<RemoteFileModel> _remoteFiles = [];
        public ObservableCollection<RemoteFileModel> RemoteFiles { get => _remoteFiles; }

        private ListCollectionView _remoteFilesView;
        public ListCollectionView RemoteFilesView { get => _remoteFilesView; }

        private ObservableCollection<RemoteFileModel> _selectedRemoteFiles = [];
        public ObservableCollection<RemoteFileModel> SelectedRemoteFiles { get => _selectedRemoteFiles; set => SetProperty(ref _selectedRemoteFiles, value); }

        public RelayCommand<string> FwUpdateCommand { get; }
        public RelayCommand<string> FwUpdateBundleCommand { get; }
        public RelayCommand<string> FileUploadCommand { get; }
        public RelayCommand<string> FileDownload { get; }
        public ICommand Loaded { get; }
        public ICommand UpdateSelectedItems { get; }
        public ICommand RepositoryMouseUp { get; }
        public RelayCommand RemoteRefreshCommand { get; }
        public RelayCommand<string> RemoteEraseCommand { get; }
        public ICommand RemoteSortCommand { get; }
        public RelayCommand RemoteCancel { get; }
        public RelayCommand RemoteSelectedEraseCommand { get; }
        public RelayCommand RemoteSelectedDownloadCommand { get; }
        public ICommand RefreshExplorer { get; }
        


        public PageFileShareViewModel()
        {
            _remoteFilesView = new ListCollectionView(RemoteFiles);

            FwUpdateCommand = new RelayCommand<string>(OnFWUpdateCommand, (o)=>!IsFileTransfering);
            FwUpdateBundleCommand = new RelayCommand<string>(OnFWUpdateBundleCommand, (o)=>!IsFileTransfering);
            FileUploadCommand = new RelayCommand<string>(OnFileUploadCommand, (o)=>!IsFileTransfering);
            FileDownload = new RelayCommand<string>(OnFileDownloadCommand, (o)=>!IsFileTransfering);
            Loaded = new RelayCommand(OnLoaded);
            UpdateSelectedItems = new RelayCommand<object>(OnUpdateSelectedItems);
            RepositoryMouseUp = new RelayCommand<MouseButtonEventArgs>(OnRepoMouseUp);
            RemoteRefreshCommand = new RelayCommand(OnRemoteRefreshCommand, ()=>!IsFileTransfering);
            RemoteEraseCommand = new RelayCommand<string>(OnRemoteEraseCommand, (o)=>!IsFileTransfering);
            RemoteSortCommand = new RelayCommand(OnRemotesSortCommand);
            RemoteCancel = new RelayCommand( async () => { await ZeroMqQueryExecutor.Instance.CancelAllQueryAsync(); IsFileTransfering = false; }, () => IsFileTransfering);
            RemoteSelectedEraseCommand = new RelayCommand(OnRemoteSelectedEraseCommand);
            RemoteSelectedDownloadCommand = new RelayCommand(OnRemoteSelectedDownloadCommand);
            RefreshExplorer = new RelayCommand(OnRefreshExplorer);


            _SlowStuffSemaphore = new SemaphoreSlim(1, 1);
            _OneTaskScheduler = new OneTaskLimitedScheduler();
            _CancelTokenSource = new CancellationTokenSource();

            FolderItemsView = FileListView.Factory.CreateFileListViewModel();
            FolderTextPath = FolderControlsLib.Factory.CreateFolderComboBoxVM();
            TreeBrowser = FolderBrowser.FolderBrowserFactory.CreateBrowserViewModel(false);

            WeakEventManager<ICanNavigate, BrowsingEventArgs>
                .AddHandler(FolderTextPath, "BrowseEvent", Control_BrowseEvent);

            WeakEventManager<ICanNavigate, BrowsingEventArgs>
                .AddHandler(FolderItemsView, "BrowseEvent", Control_BrowseEvent);

            WeakEventManager<IFileOpenEventSource, FileOpenEventArgs>
                .AddHandler(FolderItemsView, "OnFileOpen", FolderItemsView_OnFileOpen);

            WeakEventManager<ICanNavigate, BrowsingEventArgs>
                .AddHandler(TreeBrowser, "BrowseEvent", Control_BrowseEvent);


            AddFileDownListCommand = new RelayCommand(OnAddFileDownListCommand);
            RemoveFileDownListElement = new RelayCommand(OnRemoveFileDownListElement);
            AddFileDownloadPath = new RelayCommand(OnAddFileDownloadPath);
            BrowseSavePath = new RelayCommand(OnBrowseSavePath);

            StartDownloadCommand = new RelayCommand(OnStartDownloadCommand, () => IsFileDownloadStopped);
            StopDownloadCommand = new RelayCommand(OnStopDownloadCommand, () => !IsFileDownloadStopped);
            UpdateFileDownloadListItmes = new RelayCommand<object>(OnUpdateFileDownloadListItmes);

            DownloadFileMoveUp = new RelayCommand(OnDownloadFileMoveUp, () => SelectedFileDownloadItem.Count == 1);
            DownloadFileMoveDown = new RelayCommand(OnDownloadFileMoveDown, () => SelectedFileDownloadItem.Count == 1);
        }

        private async void OnSendFirmwareUpdate(string path, bool isBundle, bool isFile)
        {
            if (IsFileTransfering == true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zFileTransfering"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            FirmwareUpdatePacket firmware = new FirmwareUpdatePacket();
            firmware.SatelliteId = _satelliteId.ToString();
            firmware.ModuleMac = _moduleMac;
            firmware.BoardRevision = _boardRevision;
            firmware.CpuType = _cpuType;
            firmware.SubModule = _subModule;
            firmware.FWType = _fwType;
            firmware.FWVerMaj = _fwVerMaj;
            firmware.FWVerMin = _fwVerMin;
            firmware.ModuleType = _moduleType;
            firmware.ModuleConfig = _moduleConfig;

            firmware.FilePath = path;
            firmware.Flags = _flags;
            firmware.IsBundle = isBundle;
            firmware.IsFile = isFile;

            try
            {
                IsFileTransfering = true;
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(firmware, DispatcherType.FileTransfer);
                // TODO: Handle return value
                IsFileTransfering = false;
                RemoteRefreshCommand.Execute(null);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(ex.Message, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsFileTransfering = false;
            }
        }


        private void OnFWUpdateCommand(string? path)
        {
            if (!File.Exists(path))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zNoSuchFile"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            OnSendFirmwareUpdate(path, false, false);
        }

        private void OnFWUpdateBundleCommand(string? path)
        {
            if (!File.Exists(path))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zNoSuchFile"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            OnSendFirmwareUpdate(path, true, false);
        }

        private void OnFileUploadCommand(string? path)
        {
            if (!File.Exists(path))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zNoSuchFile"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }
            OnSendFirmwareUpdate(path, false, true);
        }

        private async Task<bool> FileEraseAsync(string path)
        {
            if (IsFileTransfering == true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zFileTransfering"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return false;
            }

            IsFileTransfering = true;

            SatelliteMethodModel _method = new SatelliteMethodModel(4, false, "obc_file_erase", "obc_file_erase", "eraseFile", 15);
            SatelliteCommandModel _command = new SatelliteCommandModel(_method);

            var filePath = FileConvertHelper.ToFixedAscii(path, 48);
            List<object> parameters = new List<object>();
            parameters.Add(filePath);
            _command.InputParameters.Add(parameters);
            _command.InputSerialized = QueryExecutorBase.Serializer(_command.InputParameters);

            try
            {
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(_command, DispatcherType.Postpone);
                if (ret is byte[] b)
                {
                    if (b.Length >= 9)
                    {
                        var fidl = BitConverter.ToUInt16(b, 0);
                        var func = BitConverter.ToUInt32(b, 2);
                        var seq = BitConverter.ToUInt16(b, 6);
                        var err = b[8];
                        b = b[9..];
                    }
                    var result = b[0];
                    string name = Enum.GetName(typeof(FManOpResult), result) ?? $"Unknown (0x{result:X2})";

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(name, TranslationSource.Instance["sInfo"], MessageBoxButton.OK, MessageBoxImage.Information);
                    });

                    IsFileTransfering = false;
                    return true;
                }
                else if (ret is string retStr)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(retStr, TranslationSource.Instance["sInfo"], MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(ex.Message, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            IsFileTransfering = false;
            return false;
        }

        private async Task<bool> FileDownloadAsync(string path, bool local_save = false)
        {
            if (IsFileTransfering == true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zFileTransfering"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return false;
            }

            // Expected path: /sd/...
            IsFileTransfering = true;
            
            SatelliteMethodModel _method = new SatelliteMethodModel(1450, false, "filedownload_cp", "filedownload_cp", "download", 0);
            SatelliteCommandModel _command = new SatelliteCommandModel(_method);
            string fileFormat = path;
            string paddedString = fileFormat.PadRight(48, '\0');

            List<object> parameters = new List<object>();
            byte[] bytes = Encoding.Default.GetBytes(paddedString);
            parameters.Add(bytes);
            _command.InputParameters.Add(parameters);
            _command.InputSerialized = QueryExecutorBase.Serializer(_command.InputParameters);

            try
            {
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(_command, DispatcherType.FileTransfer);
                if (ret is byte[] b)
                {
                    string outputPath;
                    string dir = FolderTextPath.CurrentFolder;
                    if (local_save || string.IsNullOrWhiteSpace(dir))
                    {
                        outputPath = path.Split('/').Last();
                    }
                    else
                    {
                        string full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dir));
                        Directory.CreateDirectory(full);
                        outputPath = Path.Combine(full, path.Split('/').Last());
                    }

                    await File.WriteAllBytesAsync(outputPath, b);
                    IsFileTransfering = false;
                    return true;
                }
                else if (ret is string retStr)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(retStr, TranslationSource.Instance["sInfo"], MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(ex.Message, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            IsFileTransfering = false;
            return false;
        }

        private async Task<Tuple<bool,string>> FileDownloadManagerAsync(string path)
        {
            SatelliteMethodModel _method = new SatelliteMethodModel(1450, false, "filedownload_cp", "filedownload_cp", "download", 0);
            SatelliteCommandModel _command = new SatelliteCommandModel(_method);
            string fileFormat = path;
            string paddedString = fileFormat.PadRight(48, '\0');

            List<object> parameters = new List<object>();
            byte[] bytes = Encoding.Default.GetBytes(paddedString);
            parameters.Add(bytes);
            _command.InputParameters.Add(parameters);
            _command.InputSerialized = QueryExecutorBase.Serializer(_command.InputParameters);

            try
            {
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(_command, DispatcherType.FileTransfer);
                if (ret is byte[] b)
                {
                    string outputPath;
                    string dir = SavePath;

                    if (string.IsNullOrWhiteSpace(dir))
                    {
                        outputPath = Path.Combine(FolderTextPath.CurrentFolder,path.Split('/').Last());
                    }
                    else
                    {
                        string full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dir));
                        Directory.CreateDirectory(full);
                        outputPath = Path.Combine(full, path.Split('/').Last());
                    }

                    await File.WriteAllBytesAsync(outputPath, b);

                    // Return received bytes as info
                    return new Tuple<bool, string>(true, $"{b.Length} bytes received.");
                }
                else if (ret is string retStr)
                {
                    // Return error message as info
                    return new Tuple<bool, string>(false, retStr);
                }
            }
            catch (Exception)
            {
                // Return exception message as info
                return new Tuple<bool, string>(false, "Exception occurred during file download.");
            }

            return new Tuple<bool, string>(false, "Unknown error occurred during file download.");
        }


        private async void OnFileDownloadCommand(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(TranslationSource.Instance["zNullArgument"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            if(await FileDownloadAsync(path))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var file = path.Split(Path.DirectorySeparatorChar).Last();
                    MessageBox.Show("Saved: " + file, TranslationSource.Instance["sInfo"], MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }

        private async void OnLoaded()
        {
            if (string.Empty == FolderTextPath.CurrentFolder)
            {
                NavigateToFolder(PathFactory.SysDefault);
            }
 
            await UpdateDirListAsync();
        }

        private void OnUpdateSelectedItems(object? obj)
        {
            if (obj is IList<object> list)
            {
                SelectedRemoteFiles.Clear();
                foreach (var item in list)
                {
                    if (item is RemoteFileModel model)
                    {
                        SelectedRemoteFiles.Add(model);
                    }
                }
            }
        }

        private void OnRepoMouseUp(MouseButtonEventArgs? e)
        {
            if (e == null) { return; }
            if (e.ChangedButton == MouseButton.XButton1)
            {
                BackwardCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.XButton2)
            {
                ForwardCommand.Execute(null);
                e.Handled = true;
            }
        }

        private async Task UpdateDirListAsync()
        {
            try
            {
                RemoteFiles.Clear();
                using (var reader = new StreamReader("DirList.txt"))
                {
                    var header = await reader.ReadLineAsync();
                    if (header == null || !header.StartsWith("file_name"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show("Invalid directory list file.", TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return;
                    }
                    while (!reader.EndOfStream)
                    {
                        //file_name,size(in bytes),attributes,timestamp
                        var line = await reader.ReadLineAsync();
                        if (line == null) { continue; }
                        var parts = line.Split(',');
                        if (parts.Length != 4) { continue; }


                        var name = parts[0].Split('/').Last();
                        var path = parts[0];
                        long size = 0;
                        if (!long.TryParse(parts[1], out size)) { continue; }
                        var attr = parts[2];
                        long time = 0;
                        if (!long.TryParse(parts[3], out time)) { continue; }

                        if (attr == "f")
                        {
                            RemoteFileModel model = new RemoteFileModel(name, path, size, time);
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                RemoteFiles.Add(model);
                            });
                        }
                    }
                }
            } catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(ex.Message, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        


        private async void OnRemoteRefreshCommand()
        {
            if (IsFileTransfering == true) { return; }

            IsFileTransfering = true;
            bool success = true;
            
            // Send Directory List Command
            SatelliteMethodModel _method = new SatelliteMethodModel(9, false, "obc_filelist", "obc_filelist", "updateFileList", 15);
            SatelliteCommandModel _command = new SatelliteCommandModel(_method);

            var filePath = FileConvertHelper.ToFixedAscii("/sd/DirList.txt", 48);
            var pattern = FileConvertHelper.ToFixedAscii("*", 48);

            List<object> parameters = new List<object>();
            parameters.Add(filePath);
            parameters.Add(pattern);
            _command.InputParameters.Add(parameters);
            _command.InputSerialized = QueryExecutorBase.Serializer(_command.InputParameters);
            
            try
            {
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(_command, DispatcherType.Postpone);
                if (ret is byte[] b)
                {
                    if (b.Length >= 9)
                    {
                        var fidl = BitConverter.ToUInt16(b, 0);
                        var func = BitConverter.ToUInt32(b, 2);
                        var seq = BitConverter.ToUInt16(b, 6);
                        var err = b[8];
                        b = b[9..];
                    }
                    var result = b[0];
                    string name = Enum.GetName(typeof(FManOpResult), result) ?? $"Unknown (0x{result:X2})";

                    if (result != 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show(name, TranslationSource.Instance["sInfo"], MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                        success = false;
                    }
                }
                else if (ret is string retStr)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(retStr, TranslationSource.Instance["sInfo"], MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    success = false;
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(ex.Message, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
                });
                success = false;
            }

            IsFileTransfering = false;

            // Download the file list
            if (success && !await FileDownloadAsync("/sd/DirList.txt", true))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("Failed to download directory list.", TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }

            // Update the list
            await UpdateDirListAsync();
        }

        private async void OnRemoteEraseCommand(string? path)
        {
            if(path == null || path.Length == 0) { return; }
            if (await FileEraseAsync(path))
            {
                RemoteRefreshCommand.Execute(null);
            }
        }


        static bool _sortToggle = false;
        private void OnRemotesSortCommand()
        {
            _sortToggle = !_sortToggle;
            _remoteFilesView.CustomSort = Comparer<RemoteFileModel>.Create((x, y) =>
            {
                if (x == null || y == null) return 0;

                int depthX = x.Path.Count(c => c == '/');
                int depthY = y.Path.Count(c => c == '/');

                int cmp = depthX.CompareTo(depthY);
                if (cmp != 0)
                    return cmp * (_sortToggle ? 1 : -1);

                return string.Compare(x.Path, y.Path, StringComparison.Ordinal) * (_sortToggle ? 1 : -1);

            });
        }

        private async void OnRemoteSelectedEraseCommand()
        {
            foreach(var file in SelectedRemoteFiles)
            {
                if(!await FileEraseAsync(file.Path))
                {
                    break;
                }
            }

            if (!await FileDownloadAsync("/sd/DirList.txt", true))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("Failed to download directory list.", TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }

            await UpdateDirListAsync();
        }
        private async void OnRemoteSelectedDownloadCommand()
        {
            foreach (var file in SelectedRemoteFiles)
            {
                if(!await FileDownloadAsync(file.Path))
                {
                    break;
                }
            }
        }

        private void OnRefreshExplorer()
        {
            if (string.Empty == FolderTextPath.CurrentFolder)
            {
                NavigateToFolder(PathFactory.SysDefault);
            }
            else
            {
                NavigateToFolder(PathFactory.Create(FolderTextPath.CurrentFolder));
            }
        }

        private void OnAddFileDownListCommand()
        {
            foreach (var f in SelectedRemoteFiles)
            {
                FileDownloadList.Add(new FileDownloadModel() { Path = f.Path, Name = f.Name, Status = FileDownloadStatus.Pending });
            }
        }

        private void OnRemoveFileDownListElement()
        {
            if (SelectedFileDownloadItem.Count == 0) { return; }
            foreach (var item in SelectedFileDownloadItem.ToList())
            {
                FileDownloadList.Remove(item);
            }
        }

        private void OnAddFileDownloadPath()
        {
            if (string.IsNullOrEmpty(FileDownloadPath)) { return; }
            FileDownloadList.Add(new FileDownloadModel() { Path = FileDownloadPath, Name = FileDownloadPath.Split('/').Last(), Status = FileDownloadStatus.Pending });
            FileDownloadPath = string.Empty;
        }

        private void OnBrowseSavePath()
        {
            var dialogue = new OpenFolderDialog
            {
                Title = "Select Save Path",
                InitialDirectory = SavePath,
            };

            if (dialogue.ShowDialog() == false) return;

            SavePath = dialogue.FolderName;
        }

        private void OnStartDownloadCommand()
        {
            IsFileDownloadStopped = false;
            StartDownloadCommand.NotifyCanExecuteChanged();
            StopDownloadCommand.NotifyCanExecuteChanged();

            if(FileDownloadList.Count == 0)
            {
                DownloadStatus = "No files to download.";
                IsFileDownloadStopped = true;
                StartDownloadCommand.NotifyCanExecuteChanged();
                StopDownloadCommand.NotifyCanExecuteChanged();
                return;
            }

            if(IsFileTransfering == true)
            {
                DownloadStatus = "File transfer in progress. Please wait.";
                IsFileDownloadStopped = true;
                StartDownloadCommand.NotifyCanExecuteChanged();
                StopDownloadCommand.NotifyCanExecuteChanged();
                return;
            }

            if(SavePath == string.Empty)
            {
                DownloadStatus = "Please select a save path.";
                IsFileDownloadStopped = true;
                StartDownloadCommand.NotifyCanExecuteChanged();
                StopDownloadCommand.NotifyCanExecuteChanged();
                return;
            }

            Application.Current.Dispatcher.Invoke(async () =>
            {
                //Initialize download process
                foreach (var item in FileDownloadList)
                {
                    if (IsFileDownloadStopped)
                    {
                        break;
                    }


                    if (item.Status != FileDownloadStatus.Completed)
                    {
                        item.Status = FileDownloadStatus.Pending;
                        item.ExecutedTime = string.Empty;
                        item.Info = string.Empty;
                    }
                }

                DownloadStatus = "Download started.";

                await Task.Delay(500);

                // Check Schedule
                if (IsEnableSchedule)
                {
                    // Wait until scheduled time
                    while (true)
                    {   
                        if (IsFileDownloadStopped)
                        {
                            break;
                        }

                        await Task.Delay(1000);

                        var now = DateTime.Now;
                        if (now >= SelectedScheduleDate)
                        {
                            break;
                        }
                        DownloadStatus = $"Waiting {(SelectedScheduleDate - now).ToString(@"dd\.hh\:mm\:ss")}";
                    }

                    if(IsFileDownloadStopped)
                    {
                        DownloadStatus = "Download stopped.";
                        return;
                    }
                }


                // Start download
                IsFileTransfering = true;
                foreach (var item in FileDownloadList)
                {
                    if (IsFileDownloadStopped)
                    {
                        break;
                    }
                    if (item.Status == FileDownloadStatus.Pending)
                    {
                        item.Status = FileDownloadStatus.InProgress;

                        item.ExecutedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        DownloadStatus = $"Downloading: {item.Name}";
                        var result = await FileDownloadManagerAsync(item.Path);
                        item.Info = result.Item2;

                        if (result.Item1)
                        {
                            item.Status = FileDownloadStatus.Completed;
                        }
                        else
                        {
                            item.Status = FileDownloadStatus.Failed;
                        }

                    }
                }
                IsFileTransfering = false;

                IsFileDownloadStopped = true;
                StartDownloadCommand.NotifyCanExecuteChanged();
                StopDownloadCommand.NotifyCanExecuteChanged();
                DownloadStatus = "Download stopped.";
            });
        }

        private void OnStopDownloadCommand()
        {
            IsFileDownloadStopped = true;
            StartDownloadCommand.NotifyCanExecuteChanged();
            StopDownloadCommand.NotifyCanExecuteChanged();
            DownloadStatus = "Download stopped.";
        }

        private void OnUpdateFileDownloadListItmes(object? obj)
        {
            if (obj is IList<object> list)
            {
                SelectedFileDownloadItem.Clear();
                foreach (var item in list)
                {
                    if (item is FileDownloadModel model)
                    {
                        SelectedFileDownloadItem.Add(model);
                    }
                }
                DownloadFileMoveUp.NotifyCanExecuteChanged();
                DownloadFileMoveDown.NotifyCanExecuteChanged();
            }
        }

        private void OnDownloadFileMoveUp()
        {
            if (SelectedFileDownloadItem.Count != 1) { return; }
            var item = SelectedFileDownloadItem[0];
            var index = FileDownloadList.IndexOf(item);
            if (index > 0)
            {
                FileDownloadList.Move(index, index - 1);
            }
        }

        private void OnDownloadFileMoveDown()
        {
            if (SelectedFileDownloadItem.Count != 1) { return; }
            var item = SelectedFileDownloadItem[0];
            var index = FileDownloadList.IndexOf(item);
            if (index < FileDownloadList.Count - 1)
            {
                FileDownloadList.Move(index, index + 1);
            }
        }

        #region Folder method
        /// <summary>
        /// Master controller interface method to navigate all views
        /// to the folder indicated in <paramref name="folder"/>
        /// - updates all related viewmodels.
        /// </summary>
        /// <param name="itemPath"></param>
        /// <param name="requestor"</param>
        public override void NavigateToFolder(IPathModel itemPath)
        {
            try
            {
                // XXX Todo Keep task reference, support cancel, and remove on end?
                var timeout = TimeSpan.FromSeconds(5);
                var actualTask = new Task(() =>
                {
                    var request = new BrowseRequest(itemPath, _CancelTokenSource.Token);

                    var t = Task.Factory.StartNew(() => NavigateToFolderAsync(request, null),
                                                        request.CancelTok,
                                                        TaskCreationOptions.LongRunning,
                                                        _OneTaskScheduler);


                    if (t.Wait(timeout) == true)
                        return;

                    _CancelTokenSource.Cancel();       // Task timed out so lets abort it
                    return;                     // Signal timeout here...
                });

                actualTask.Start();
                actualTask.Wait();
            }
            catch (System.AggregateException e)
            {
                Debug.WriteLine(e);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        /// <summary>
		/// Master controler interface method to navigate all views
		/// to the folder indicated in <paramref name="folder"/>
		/// - updates all related viewmodels.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="requestor"</param>
		private async Task<FinalBrowseResult> NavigateToFolderAsync(BrowseRequest request,
                                                                    object? sender)
        {
            // Make sure the task always processes the last input but is not started twice
            await _SlowStuffSemaphore.WaitAsync();
            try
            {
                var newPath = request.NewLocation;
                var cancel = request.CancelTok;

                if (cancel != null)
                    cancel.ThrowIfCancellationRequested();

                TreeBrowser.SetExternalBrowsingState(true);
                FolderItemsView.SetExternalBrowsingState(true);
                FolderTextPath.SetExternalBrowsingState(true);

                FinalBrowseResult browseResult = null;

                // Navigate TreeView to this file system location
                if (TreeBrowser != sender)
                {
                    browseResult = await TreeBrowser.NavigateToAsync(request);

                    if (cancel != null)
                        cancel.ThrowIfCancellationRequested();

                    if (browseResult.Result != BrowseResult.Complete)
                        return FinalBrowseResult.FromRequest(request, BrowseResult.InComplete);
                }

                // Navigate Folder ComboBox to this folder
                if (FolderTextPath != sender)
                {
                    browseResult = await FolderTextPath.NavigateToAsync(request);

                    if (cancel != null)
                        cancel.ThrowIfCancellationRequested();

                    if (browseResult.Result != BrowseResult.Complete)
                        return FinalBrowseResult.FromRequest(request, BrowseResult.InComplete);
                }

                if (cancel != null)
                    cancel.ThrowIfCancellationRequested();

                // Navigate Folder/File ListView to this folder
                if (FolderItemsView != sender)
                {
                    browseResult = await FolderItemsView.NavigateToAsync(request);

                    if (cancel != null)
                        cancel.ThrowIfCancellationRequested();

                    if (browseResult.Result != BrowseResult.Complete)
                        return FinalBrowseResult.FromRequest(request, BrowseResult.InComplete);
                }

                if (browseResult != null)
                {
                    if (browseResult.Result == BrowseResult.Complete)
                    {
                        SelectedFolder = newPath.Path;

                        // Log location into history of recent locations
                        NaviHistory.Forward(newPath);
                    }
                }

                return browseResult;
            }
            catch (Exception exp)
            {
                var result = FinalBrowseResult.FromRequest(request, BrowseResult.InComplete);
                result.UnexpectedError = exp;
                return result;
            }
            finally
            {
                TreeBrowser.SetExternalBrowsingState(true);
                FolderItemsView.SetExternalBrowsingState(false);
                FolderTextPath.SetExternalBrowsingState(false);

                _SlowStuffSemaphore.Release();
            }
        }

        /// <summary>
		/// Executes when the file open event is fired and class was constructed with statndard constructor.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected override void FolderItemsView_OnFileOpen(object sender, FileOpenEventArgs e)
        {
            MessageBox.Show("File Open:" + e.FileName);
        }

        /// <summary>
        /// A control has send an event that it has (been) browsing to a new location.
        /// Lets sync this with all other controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_BrowseEvent(object sender, FileSystemModels.Browse.BrowsingEventArgs e)
        {
            var location = e.Location;

            SelectedFolder = location.Path;

            if (e.IsBrowsing == false && e.Result == BrowseResult.Complete)
            {
                // XXX Todo Keep task reference, support cancel, and remove on end?
                try
                {
                    var timeout = TimeSpan.FromSeconds(5);
                    var actualTask = new Task(() =>
                    {
                        var request = new BrowseRequest(location, _CancelTokenSource.Token);

                        var t = Task.Factory.StartNew(() => NavigateToFolderAsync(request, sender),
                                                            request.CancelTok,
                                                            TaskCreationOptions.LongRunning,
                                                            _OneTaskScheduler);

                        if (t.Wait(timeout) == true)
                            return;

                        _CancelTokenSource.Cancel();           // Task timed out so lets abort it
                        return;                         // Signal timeout here...
                    });

                    actualTask.Start();
                    actualTask.Wait();
                }
                catch (System.AggregateException ex)
                {
                    Debug.WriteLine(ex);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            else
            {
                if (e.IsBrowsing == true)
                {
                    // The sender has messaged: "I am changing location..."
                    // So, we set this property to tell the others:
                    // 1) Don't change your location now (eg.: Disable UI)
                    // 2) We'll be back to tell you the location when we know it
                    if (TreeBrowser != sender)
                        TreeBrowser.SetExternalBrowsingState(true);

                    if (FolderTextPath != sender)
                        FolderTextPath.SetExternalBrowsingState(true);

                    if (FolderItemsView != sender)
                        FolderItemsView.SetExternalBrowsingState(true);
                }
            }
        }

        #endregion

        #region Disposable Interfaces
        /// <summary>
        /// Standard dispose method of the <seealso cref="IDisposable" /> interface.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Source: http://www.codeproject.com/Articles/15360/Implementing-IDisposable-and-the-Dispose-Pattern-P
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == false)
            {
                if (disposing == true)
                {
                    // Dispose of the curently displayed content
                    _OneTaskScheduler.Dispose();
                    _SlowStuffSemaphore.Dispose();
                    _CancelTokenSource.Dispose();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            _disposed = true;

            //// If it is available, make the call to the
            //// base class's Dispose(Boolean) method
            ////base.Dispose(disposing);
        }
        #endregion Disposable Interfaces

        
    }
}
