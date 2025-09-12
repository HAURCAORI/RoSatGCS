using CommunityToolkit.Mvvm.Input;
using FileListView.Interfaces;
using FileSystemModels;
using FileSystemModels.Browse;
using FileSystemModels.Events;
using FileSystemModels.Interfaces;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Utils.Query;
using RoSatGCS.Utils.Satellites.TLE;
using RoSatGCS.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PageFileShareViewModel : FileTreeListControllerViewModel, IDisposable
    {
        private readonly SemaphoreSlim _SlowStuffSemaphore;
        private readonly OneTaskLimitedScheduler _OneTaskScheduler;
        private readonly CancellationTokenSource _CancelTokenSource;
        private bool _disposed = false;


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
        private string _fwFilePath = string.Empty;
        private string _fileName = string.Empty;

        public string SatId
        {
            get => _satelliteId.ToString();
            set
            {
                if (UInt64.TryParse(value, out var ret))
                    SetProperty(ref _satelliteId, ret);
            }
        }
        public string ModuleMac
        {
            get => _moduleMac.ToString();
            set
            {
                if (byte.TryParse(value, out var ret))
                    SetProperty(ref _moduleMac, ret);
            }
        }

        public string BoardRevision
        {
            get => _boardRevision.ToString();
            set
            {
                if (UInt16.TryParse(value, out var ret))
                    SetProperty(ref _boardRevision, ret);
            }
        }

        public string CpuType
        {
            get => _cpuType.ToString();
            set
            {
                if (UInt16.TryParse(value, out var ret))
                    SetProperty(ref _cpuType, ret);
            }
        }

        public string SubModule
        {
            get => _subModule.ToString();
            set
            {
                if (UInt16.TryParse(value, out var ret))
                    SetProperty(ref _subModule, ret);
            }
        }

        public string FwType
        {
            get => _fwType.ToString();
            set
            {
                if (UInt16.TryParse(value, out var ret))
                    SetProperty(ref _fwType, ret);
            }
        }

        public string FwVerMaj
        {
            get => _fwVerMaj.ToString();
            set
            {
                if (UInt16.TryParse(value, out var ret))
                    SetProperty(ref _fwVerMaj, ret);
            }
        }

        public string FwVerMin
        {
            get => _fwVerMin.ToString();
            set
            {
                if (UInt16.TryParse(value, out var ret))
                    SetProperty(ref _fwVerMin, ret);
            }
        }

        public string ModuleType
        {
            get => _moduleType.ToString();
            set
            {
                if (UInt16.TryParse(value, out var ret))
                    SetProperty(ref _moduleType, ret);
            }
        }

        public string ModuleConfig
        {
            get => _moduleConfig.ToString();
            set
            {
                if (UInt16.TryParse(value, out var ret))
                    SetProperty(ref _moduleConfig, ret);
            }
        }

        public string Flags
        {
            get => _flags.ToString();
            set
            {
                if (UInt64.TryParse(value, out var ret))
                    SetProperty(ref _flags, ret);
            }
        }

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string FwFilePath { get => _fwFilePath; set => SetProperty(ref _fwFilePath, value); }


        public ICommand OpenFile { get; }
        public ICommand FwUpdateCommand { get; }
        public ICommand FwUpdateBundleCommand { get; }
        public ICommand FileUploadCommand { get; }
        public ICommand FileDownload { get; }
        public ICommand Loaded { get; }
        public ICommand RepositoryMouseUp { get; }


        public PageFileShareViewModel()
        {
            OpenFile = new RelayCommand(OnOpenFile);
            FwUpdateCommand = new RelayCommand(OnFWUpdateCommand);
            FwUpdateBundleCommand = new RelayCommand(OnFWUpdateBundleCommand);
            FileUploadCommand = new RelayCommand(OnFileUploadCommand);
            FileDownload = new RelayCommand(OnFileDownloadCommand);
            Loaded = new RelayCommand(OnLoaded);
            RepositoryMouseUp = new RelayCommand<MouseButtonEventArgs>(OnRepoMouseUp);


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

        }

        private async void OnSendFirmwareUpdate(bool isBundle, bool isFile)
        {
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
            firmware.FilePath = _fwFilePath;
            firmware.Flags = _flags;
            firmware.IsBundle = isBundle;
            firmware.IsFile = isFile;

            try
            {
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(firmware, DispatcherType.FileTransfer);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenFile()
        {
            var dialogue = new WindowFileSearch(["*"]);
            if (dialogue.ShowDialog() == true)
            {
                this.FwFilePath = dialogue.Path;
            }
        }

        private void OnFWUpdateCommand()
        {
            if (string.IsNullOrEmpty(FwFilePath))
            {
                System.Windows.MessageBox.Show(TranslationSource.Instance["zNullArgument"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OnSendFirmwareUpdate(false, false);
        }

        private void OnFWUpdateBundleCommand()
        {
            if (string.IsNullOrEmpty(FwFilePath))
            {
                System.Windows.MessageBox.Show(TranslationSource.Instance["zNullArgument"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OnSendFirmwareUpdate(true, false);
        }

        private void OnFileUploadCommand()
        {
            if (string.IsNullOrEmpty(FwFilePath))
            {
                System.Windows.MessageBox.Show(TranslationSource.Instance["zNullArgument"], TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OnSendFirmwareUpdate(false, true);
        }

        private async void OnFileDownloadCommand()
        {
            SatelliteMethodModel _method = new SatelliteMethodModel(1450, false, "filedownload_cp", "filedownload_cp", "download", 0);
            SatelliteCommandModel _command = new SatelliteCommandModel(_method);
            string fileFormat = "/sd/" + FileName;
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
                    string outputPath = FileName;
                    await File.WriteAllBytesAsync(outputPath, b);
                    MessageBox.Show(outputPath + ": Saved(" + b.Length + ")", TranslationSource.Instance["sInfo"], MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnLoaded()
        {
            var path = PathFactory.SysDefault;
            NavigateToFolder(path);
        }

        private void OnRepoMouseUp(MouseButtonEventArgs e)
        {
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
                                                                    object sender)
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
        private void Control_BrowseEvent(object sender,
                                                FileSystemModels.Browse.BrowsingEventArgs e)
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
