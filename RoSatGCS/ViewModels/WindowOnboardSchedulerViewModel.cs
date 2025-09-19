using CommunityToolkit.Mvvm.Input;
using MessagePack;
using Microsoft.Win32;
using RoSatGCS.Models;
using RoSatGCS.Utils.Converter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class WindowOnboardSchedulerViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly string PathCacheSchedule = "Cache/schedule";
        private static readonly uint HeaderSize = 12;
        private static readonly uint CPHeaderSize = 10;
        private static readonly uint FPHeaderSize = 18;
        private static readonly uint Timeout = 2000; // milliseconds
        private static uint seq = 0;

        private string _fileName = "";
        public string FileName { get => _fileName; set => SetProperty(ref _fileName, value); }

        private ListCollectionView _satScheduleCommandView;
        public ListCollectionView SatelliteScheduleCommandView { get => _satScheduleCommandView; }

        private SatelliteCommandModel? _selectedCommand;
        public SatelliteCommandModel? SelectedCommand
        {
            get => _selectedCommand; set
            {
                SetProperty(ref _selectedCommand, value);
                OnPropertyChanged(nameof(CommandFile));
                OnPropertyChanged(nameof(CommandFIDLId));
                OnPropertyChanged(nameof(CommandId));
                OnPropertyChanged(nameof(CommandName));
                OnPropertyChanged(nameof(CommandTarget));
                OnPropertyChanged(nameof(CommandIsCP));
                OnPropertyChanged(nameof(CommandTimestamp));
            }
        }

        public string CommandFile { get => (SelectedCommand != null) ? SelectedCommand.File : "-"; }
        public string CommandFIDLId { get => (SelectedCommand != null) ? SelectedCommand.FIDLId.ToString() : "-"; }
        public string CommandId { get => (SelectedCommand != null) ? SelectedCommand.Id.ToString() : "-"; }
        public string CommandName { get => (SelectedCommand != null) ? SelectedCommand.Name : "-"; }

        private List<byte> _commandTargetList = new List<byte> { 0x11, 0x33, 0x44, 0x66, 0x77};
        public List<byte> CommandTargetList { get => _commandTargetList; }

        public byte CommandTarget {
            get => (SelectedCommand != null) ? SelectedCommand.Module : (byte) 0x00;
            set
            {
                if (SelectedCommand != null)
                {
                    SelectedCommand.Module = value;
                    OnPropertyChanged(nameof(CommandTarget));
                }
            }
        }
        public bool CommandIsCP {
            get => (SelectedCommand != null) ? SelectedCommand.IsCP : false;
            set
            {
                if (SelectedCommand != null)
                {
                    SelectedCommand.IsCP = value;
                    OnPropertyChanged(nameof(CommandIsCP));
                }
            }
        }

        private DateTime _commandDateTime = DateTime.UtcNow;

        public DateTime CommandTimestamp
        {
            get
            {
                uint val = SelectedCommand?.Timestamp ?? uint.MaxValue;
                
                _commandDateTime = val == uint.MaxValue ? DateTime.UtcNow : DateTimeOffset.FromUnixTimeSeconds(val).UtcDateTime;
                return _commandDateTime;
            }
            set
            {
                if (SelectedCommand != null)
                {
                    _commandDateTime = value;
                    _commandDateTime = DateTime.SpecifyKind(_commandDateTime, DateTimeKind.Utc);
                    SelectedCommand.Timestamp = (uint) new DateTimeOffset(_commandDateTime).ToUnixTimeSeconds();
                    OnPropertyChanged(nameof(CommandTimestamp));
                }
            }
        }

        public ICommand DeleteAllCommand { get; }
        public ICommand TimestampReset { get; }
        public ICommand RemoveSelectedCommand { get; }
        public ICommand MoveCommandUp { get; }
        public ICommand MoveCommandDown { get; }
        public ICommand OpenCommandProperty { get; }
        public ICommand UpdateSchedule { get; }
        public ICommand Close { get; }
        public ICommand Export { get; }
        public ICommand Save { get; }
        public ICommand Load { get; }
        public Action? CloseAction { get; set; }
        public Action? CloseHexEditor { get; set; }
        public Action<string>? OpenHexEditor { get; set; }
        public Action<int,int>? HighlightHexEditor { get; set; }
        public Action? SaveHexEditor { get; set; }


        public WindowOnboardSchedulerViewModel()
        {
            _satScheduleCommandView = (ListCollectionView) CollectionViewSource.GetDefaultView(MainDataContext.Instance.SatelliteScheduleCommand);

            DeleteAllCommand = new RelayCommand(OnDeleteAllCommand);
            TimestampReset = new RelayCommand(() => { CommandTimestamp = DateTimeOffset.FromUnixTimeSeconds(0).UtcDateTime; });
            RemoveSelectedCommand = new RelayCommand(OnRemoveSelectedCommand);
            MoveCommandUp = new RelayCommand(OnMoveCommandUp);
            MoveCommandDown = new RelayCommand(OnMoveCommandDown);
            OpenCommandProperty = new RelayCommand(OnOpenCommandProperty);
            UpdateSchedule = new RelayCommand(OnUpdateSchedule);
            Close = new RelayCommand(() => { if (CloseAction != null) CloseAction(); });
            Export = new RelayCommand(OnExport);
            Save = new RelayCommand(OnSave);
            Load = new RelayCommand(OnLoad);    
        }
        public void OnDeleteAllCommand()
        {
            MainDataContext.Instance.SatelliteScheduleCommand.Clear();
            CloseHexEditor?.Invoke();
        }

        private void OnRemoveSelectedCommand()
        {
            if(SelectedCommand != null && MainDataContext.Instance.SatelliteScheduleCommand.Contains(SelectedCommand))
            {
                MainDataContext.Instance.SatelliteScheduleCommand.Remove(SelectedCommand);
                OnPropertyChanged(nameof(SatelliteScheduleCommandView));
            }
        }
        private void OnMoveCommandUp()
        {
            if (SelectedCommand != null)
            {
                int index = MainDataContext.Instance.SatelliteScheduleCommand.IndexOf(SelectedCommand);
                if (index > 0)
                {
                    MainDataContext.Instance.SatelliteScheduleCommand.Move(index, index - 1);
                }
                OnPropertyChanged(nameof(SatelliteScheduleCommandView));
            }
        }

        private void OnMoveCommandDown()
        {
            if (SelectedCommand != null)
            {
                int index = MainDataContext.Instance.SatelliteScheduleCommand.IndexOf(SelectedCommand);
                if (index < MainDataContext.Instance.SatelliteScheduleCommand.Count - 1 && index >= 0)
                {
                    MainDataContext.Instance.SatelliteScheduleCommand.Move(index, index + 1);
                }
                OnPropertyChanged(nameof(SatelliteScheduleCommandView));
            }
        }

        private void OnOpenCommandProperty()
        {
            if (SelectedCommand != null)
            {
                MainWindowViewModel? mainWindowViewModel = App.Current.MainWindow?.DataContext as MainWindowViewModel;
                if (mainWindowViewModel != null)
                {
                    mainWindowViewModel.OnWindowOpenFunctionProperty(SelectedCommand, OnCloseEventHandler);
                }
            }
        }

        private void OnCloseEventHandler(object? sender, EventArgs args)
        {
            //OnPropertyChanged(nameof(SelectedCommand.InputSerialized));
            //return;
        }


        private async Task<MemoryStream> CreateSlot(SatelliteCommandModel model, uint current_index, bool is_last)
        {
            // Header
            // 4 bytes: next slot offset
            // 4 bytes: timestamp
            // 4 bytes: sequence id
            // 1 byte : flag(fixed to 0)
            // 2 bytes: command size (without header)
            MemoryStream stream = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(stream);
            uint index = 0;
            bw.Write((uint)0);               index += 4; // next slot offset (to be updated later)
            bw.Write((uint)model.Timestamp); index += 4; // timestamp
            bw.Write((uint)seq++);           index += 4; // sequence id
            bw.Write((byte)0);               index += 1; // flag


            List<byte> data = model.InputSerialized;
            uint len = (uint)(data.Count);
            uint current_slot_index = index;
            uint cmd_size = 0;
            if (model.IsCP)
            {
                // Command Packet Size
                cmd_size = CPHeaderSize + len;
                bw.Write((ushort)cmd_size);     index += 2;

                // Command Packet
                bw.Write((byte)1);              index += 1; // Comm IF type
                bw.Write((byte)4);              index += 1; // Comm param size
                bw.Write((uint)model.Gateway);  index += 4; //Command ID
                bw.Write((uint)Timeout);        index += 4; // Timeout

                // CP Packet Payload
                bw.Write(data.ToArray());       index += len;
            }
            else
            {
                // Command Packet Size
                cmd_size = FPHeaderSize + len;
                bw.Write((ushort)cmd_size);     index += 2;

                // Slot FP Header
                bw.Write((byte)0);              index += 1; // Comm IF type
                bw.Write((byte)3);              index += 1; // Comm param size
                bw.Write((byte)0);              index += 1; // Mac IF ID
                bw.Write((byte)model.Module);   index += 1; // Target address
                bw.Write((byte)7);              index += 1; // Priority
                bw.Write((uint)Timeout);        index += 4; // Timeout

                // FP Packet Header
                bw.Write((ushort)model.FIDLId); index += 2; // FIDL ID
                bw.Write((uint)model.Id);       index += 4; // Command ID
                bw.Write((ushort)seq);          index += 2; // Sequence
                bw.Write((byte)0);              index += 1; // Error Code

                // FP Packet Payload
                bw.Write(data.ToArray());       index += len;
            }

            byte[] crc_data = new byte[index - 4]; // Ignore first next slot offset
            stream.Seek(4, SeekOrigin.Begin);
            stream.Read(crc_data, 0, (int) index - 4);
            stream.Seek(index, SeekOrigin.Begin);
            var crc   = CRC16_CCITT.Compute(crc_data);

            bw.Write((ushort)crc);              index += 2; // CRC16


            // Write next slot offset using total_size
            //if (!is_last)
            //{
                bw.Seek(0, SeekOrigin.Begin);
                bw.Write((uint)(index + current_index));
            //}

            return stream;
        }
        private async void OnUpdateSchedule()
        {
            List<uint> highlight = new();
            try
            {
                CloseHexEditor?.Invoke();

                seq = 0;
                using var fs = new FileStream(PathCacheSchedule, FileMode.Create, FileAccess.Write, FileShare.None);
                using var bw = new BinaryWriter(fs);

                bw.Write(new byte[] { 0x53, 0x43, 0x48, 0x45, 0x44, 0x00 }); // "SCHED\0"
                bw.Write((byte)0x00); // Version major
                bw.Write((byte)0x01); // Version minor
                bw.Write((uint)0x00); // Total file size TBD
                uint index = HeaderSize;

                foreach (var o in MainDataContext.Instance.SatelliteScheduleCommand)
                {
                    bool is_last = o == MainDataContext.Instance.SatelliteScheduleCommand.Last();
                    var slot = await CreateSlot(o, index, is_last);
                    var slotArray = slot.ToArray();
                    bw.Write(slotArray);
                    highlight.Add(index);
                    index += (uint)slotArray.Length;
                }

                // Total file size
                bw.Seek(8, SeekOrigin.Begin);
                bw.Write((uint)(index));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);

            }

            
            OpenHexEditor?.Invoke(PathCacheSchedule);
            foreach(var h in highlight)
            {
                HighlightHexEditor?.Invoke((int)h, 4);
            }
        }
        private void OnExport()
        {
            SaveHexEditor?.Invoke();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Schedule Files (*.sch)|*.sch|All files (*.*)|*.*";
            sfd.FileName = "schedule.sch";
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    File.Copy(PathCacheSchedule, sfd.FileName, true);
                }
                catch (Exception)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Failed to export schedule file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }

        private void OnSave()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Command List File (*.dat)|*.dat|All files (*.*)|*.*";
            sfd.FileName = "commandlist.dat";
            if(sfd.ShowDialog() == true)
            {
                try
                {
                    using (var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        MessagePackSerializer.Serialize(fs, MainDataContext.Instance.SatelliteScheduleCommand.ToList());
                    }
                }
                catch (Exception)
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        MessageBox.Show("Failed to save command list file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }

        private void OnLoad()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Command List File (*.dat)|*.dat|All files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    CloseHexEditor?.Invoke();

                    using (var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var list = MessagePackSerializer.Deserialize<List<SatelliteCommandModel>>(fs);
                        MainDataContext.Instance.SatelliteScheduleCommand.Clear();
                        foreach (var item in list)
                        {
                            MainDataContext.Instance.SatelliteScheduleCommand.Add(item);
                        }
                    }
                }
                catch (Exception)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Failed to load command list file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }
    }
}
