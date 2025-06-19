using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Utils.Query;
using RoSatGCS.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PageFileShareViewModel : ViewModelPageBase
    {

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
        public ICommand TEMP_OPEN_TLE { get; } // TODO: Remove this command

        public PageFileShareViewModel()
        {
            OpenFile = new RelayCommand(OnOpenFile);
            FwUpdateCommand = new RelayCommand(OnFWUpdateCommand);
            FwUpdateBundleCommand = new RelayCommand(OnFWUpdateBundleCommand);
            FileUploadCommand = new RelayCommand(OnFileUploadCommand);
            FileDownload = new RelayCommand(OnFileDownloadCommand);

            // MUST BE REMOVED
            TEMP_OPEN_TLE = new RelayCommand(ON_TEMP_OPEN_TLE);
        }

        static WindowTLE? _windowTLE = null;
        private void ON_TEMP_OPEN_TLE()
        {
            if(_windowTLE == null)
            {
                _windowTLE = new WindowTLE();
                _windowTLE.Closed += (s, e) => { _windowTLE = null; };
                _windowTLE.Show();
            }
            else
            {
                _windowTLE.Activate();
            }
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
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(firmware, DispatcherType.Postpone);
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
            parameters.Add(paddedString);
            _command.InputParameters.Add(parameters);

            try
            {
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(_command, DispatcherType.Postpone);
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
    }
}
