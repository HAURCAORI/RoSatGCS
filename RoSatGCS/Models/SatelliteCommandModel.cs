using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePack;
using NLog;
using RoSatGCS.Controls;
using RoSatGCS.Utils.Query;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Utils.Timer;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Documents;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class SatelliteCommandModel : SatelliteMethodModel, ICloneable, IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool _isMessageBoxShown = false;

        public event EventHandler? Received;


        #region Fields
        [IgnoreMember]
        private bool _disposed = false;
        [IgnoreMember]
        private List<byte> _inputSerialized = [];
        [Key("parameters")]
        private List<List<object>> _inputParameters = [];
        [Key("out")]
        private List<List<object>> _outputParameters = [];
        [Key("result")]
        private ObservableCollection<ParameterModel> _resultParameters = [];

        [Key("index")]
        private int _groupIndex = 0;
        [Key("group")]
        private string? _groupName = null;
        [Key("temp")]
        private bool _isTemp = false;
        [Key("vald")]
        private bool _isValid = false;
        [IgnoreMember]
        private bool _isexecuting = false;
        [Key("gate")]
        private ushort _gateway = 0;
        [Key("module")]
        private byte _module = 0;
        [Key("cp")]
        private bool _isCp = false;
        [Key("time")]
        private UInt32 _timestamp = 0;
        #endregion

        #region Properties
        [IgnoreMember]
        public List<byte> InputSerialized { get => _inputSerialized; internal set => SetProperty(ref _inputSerialized, value); }
        [IgnoreMember]
        public List<List<object>> InputParameters { get => _inputParameters; internal set => _inputParameters = value; }
        [IgnoreMember]
        public List<List<object>> OutputParameters { get => _outputParameters; internal set => _outputParameters = value; }
        [IgnoreMember]
        public ObservableCollection<ParameterModel> ResultParameters { get => _resultParameters; internal set => _resultParameters = value; }
        [IgnoreMember]
        public int GroupIndex { get => _groupIndex; internal set => SetProperty(ref _groupIndex, value); }
        [IgnoreMember]
        public string? GroupName { get => _groupName; internal set => SetProperty(ref _groupName, value); }
        [IgnoreMember]
        public bool IsTemp { get => _isTemp; internal set => SetProperty(ref _isTemp, value); }
        [IgnoreMember]
        public bool IsValid { get => _isValid; internal set => SetProperty(ref _isValid, value); }
        [IgnoreMember]
        public bool IsExecuting { get => _isexecuting; internal set => SetProperty(ref _isexecuting, value); }
        [IgnoreMember]
        public int InputSize
        {
            get
            {
                return _inputParameters.Sum(o =>
                {
                    static int selector(object p) => Helper.GetTypeSize(p);
                    return o.Sum(selector);
                });
            }
        }

        [IgnoreMember]
        public ushort Gateway { get => _gateway; internal set => SetProperty(ref _gateway, value); }
        [IgnoreMember]
        public byte Module { get => _module; set => SetProperty(ref _module, value); }
        [IgnoreMember]
        public bool IsCP { get => _isCp; set => SetProperty(ref _isCp, value); }   
        [IgnoreMember]
        public UInt32 Timestamp { get => _timestamp; set => SetProperty(ref _timestamp, value); }
        #endregion

        #region Commands
        [IgnoreMember]
        private RelayCommand? _execute;
        [IgnoreMember]
        public ICommand Execute { get => _execute ??= new RelayCommand(OnExecute); }
        #endregion

        #region Constructors
        protected SatelliteCommandModel() : base() { }
        public SatelliteCommandModel(SatelliteMethodModel method) : base(method.Id, method.Visibility, method.File, method.Name, method.Description, method.FIDLId)
        {
            var copy = (SatelliteMethodModel)method.Clone();

            
            if (method.File.ToLower().Contains("uhf"))
            {
                Module = 0x11;
                Gateway = 1300;
            }
            else if (method.File.ToLower().Contains("band"))
            {
                Module = 0x44;
                Gateway = 1300;
            }
            else if (method.File.ToLower().Contains("bp"))
            {
                Module = 0x66;
                //Module = 0x33;
                Gateway = 1212;
            }
            else if (method.File.ToLower().Contains("obc"))
            {
                Module = 0x33;
                Gateway = 1212;
            }
            else if (method.File.ToLower().Contains("pdm"))
            {
                Module = 0x77;
                //Module = 0x33;
                Gateway = 1212;
            }
            else if (method.File.ToLower().Contains("filedownload"))
            {
                Module = 0x33;
                Gateway = 1450;
                IsCP = true;
            }
            else
            {
                Module = 0x33;
                Gateway = 1212;
            }

            if (method.File.ToLower().Contains("cp"))
            {
                Gateway = (ushort)method.Id;
                IsCP = true;
            }


            _methodIn = new List<ParameterModel>(copy.MethodIn);
            _methodOut = new List<ParameterModel>(copy.MethodOut);
            _associatedType = new Dictionary<string, SatelliteFunctionTypeModel>(copy.AssociatedType);
            if (_methodIn.Count() == 0)
            {
                IsValid = true;
            }

            InitializeResultParameters();
        }


        #endregion

        #region Implementations

        private List<List<object>> ConvertBack(List<byte> Serialized)
        {
            List<List<object>> ret = [];
            int index = 0;
            foreach (var output in MethodOut)
            {
                if (output.DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;

                if (output.DataType == SatelliteFunctionFileModel.DataType.UserDefined)
                {
                    if (output.BaseType == SatelliteFunctionTypeModel.ArgumentType.Struct)
                    {
                        Func<ParameterModel, int>? GetParameter = null;
                        GetParameter = (val) =>
                        {
                            int j = 0;
                            if (val.BaseType == SatelliteFunctionTypeModel.ArgumentType.Struct && val.DataType == SatelliteFunctionFileModel.DataType.UserDefined)
                            {
                                AssociatedType.TryGetValue(val.UserDefinedType, out SatelliteFunctionTypeModel? tmp);
                                if (tmp is null)
                                {
                                    throw new InvalidDataException();
                                }


                                tmp.Parameters.ForEach(p =>
                                {
                                    if (p.BaseType == SatelliteFunctionTypeModel.ArgumentType.Struct && p.DataType == SatelliteFunctionFileModel.DataType.UserDefined)
                                    {
                                        if (GetParameter is not null)
                                        {
                                            if (p.IsArray)
                                            {
                                                int k = 0;
                                                while (k < p.ByteSize)
                                                    k += GetParameter(p);
                                                j += k;
                                            }
                                            else
                                            {
                                                j += GetParameter(p);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if ((index + p.ByteSize) > Serialized.Count) { return; }
                                        ret.Add(ParameterModel.ConvertValue(p.DataType, Serialized.GetRange(index, p.ByteSize), p.BaseType));
                                        index += p.ByteSize;
                                        j += p.ByteSize;
                                    }
                                });
                            }
                            else
                            {
                                if ((index + val.ByteSize) > Serialized.Count) { return 0; }
                                ret.Add(ParameterModel.ConvertValue(val.DataType, Serialized.GetRange(index, val.ByteSize), val.BaseType));
                                index += val.ByteSize;
                                j += val.ByteSize;
                            }
                            return j;
                        };

                        if (output.IsArray)
                        {
                            int j = 0;
                            while(j < output.ByteSize)
                                j += GetParameter(output);
                        }
                        else {
                            GetParameter(output);
                        }

                    }
                    else if (output.BaseType == SatelliteFunctionTypeModel.ArgumentType.Enum)
                    {
                        ret.Add(ParameterModel.ConvertValue(SatelliteFunctionFileModel.DataType.Enumeration, Serialized.GetRange(index, output.ByteSize)));
                        index += output.ByteSize;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    // NOTICE: MAYBE STRING HANDLER NEEDED
                    if (output.DataType == SatelliteFunctionFileModel.DataType.String)
                    {
                        // output.ByteSize is max size
                        // compare serialized size and ByteSize
                        int strLen = output.ByteSize;
                        if ((index + strLen) > Serialized.Count)
                            strLen = Serialized.Count - index;

                        ret.Add(ParameterModel.ConvertValue(output.DataType, Serialized.GetRange(index, strLen)));
                        index += output.ByteSize;
                        continue;
                    }
                    
                    ret.Add(ParameterModel.ConvertValue(output.DataType, Serialized.GetRange(index, output.ByteSize)));
                    index += output.ByteSize;
                }
            }
            return ret;
        }

        private async void OnExecute()
        {
            if(!IsValid)
            {
                MessageBoxResult result = await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(TranslationSource.Instance["zParametersInvalid"] + "\r\n" + TranslationSource.Instance["zExecuteAnyway"], TranslationSource.Instance["sInvalid"],
                MessageBoxButton.OKCancel, MessageBoxImage.Question));

                if (result != MessageBoxResult.OK)
                    return;
            }

            string byte_hex = string.Join(" ", InputSerialized.Select(b => b.ToString("X2")));
            ShowMessageOnce(() => MessageBox.Show(byte_hex, "Info", MessageBoxButton.OK));

            IsExecuting = true;
            try
            {
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(this, DispatcherType.Postpone);
                if (ret is byte[] b)
                {

                    if(Gateway == 1212 || Gateway == 1300)
                    {
                        if (b.Length >= 9)
                        {
                            var fidl = BitConverter.ToUInt16(b, 0);
                            var func = BitConverter.ToUInt32(b, 2);
                            var seq = BitConverter.ToUInt16(b, 6);
                            var err = b[8];
                            b = b[9..];
                        }
                    }

                    OutputParameters = ConvertBack([.. b]);
                    Received?.Invoke(this, new EventArgs());

                    foreach (var param in ResultParameters)
                    {
                        param.Value = OutputParameters[ResultParameters.IndexOf(param)];
                        param.ReceivedEvent(null);
                    }
                }
                else if (ret is string s)
                    MessageBox.Show(s);

                IsExecuting = false;
            }
            catch (TimeoutException)
            {
                IsExecuting = false;
                Logger.Error("Execution Timeout:" + this.Name);
                ShowMessageOnce(() => MessageBox.Show(TranslationSource.Instance["zExecutionTimeout"], TranslationSource.Instance["sError"]
                    , MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception e)
            {
                IsExecuting = false;
                Logger.Error("Error while executing command" + this.Name);
                ShowMessageOnce(() => MessageBox.Show(TranslationSource.Instance["zFailToExecute"] + "\r\n" + e.Message, TranslationSource.Instance["sError"]
                    , MessageBoxButton.OK, MessageBoxImage.Error));
            }
            
        }
        private static void ShowMessageOnce(Action showMessageAction)
        {
            if (_isMessageBoxShown) return;

            _isMessageBoxShown = true;
            Application.Current.Dispatcher.Invoke(() =>
            {
                showMessageAction.Invoke();
                _isMessageBoxShown = false;
            });
        }
        private void InitializeResultParameters()
        {
            foreach (var param in MethodOut)
            {
                param.CommandModel = this;
                param.IsReadOnly = true;

                foreach (var t in ParameterModel.GetType(param))
                {
                    if(t.DataType == SatelliteFunctionFileModel.DataType.None) { continue; }
                    t.CommandModel = this;
                    ResultParameters.Add(t);
                }
            }
        }

        public void Init()
        {
            foreach (var param in MethodIn)
            {
                param.CommandModel = this;
            }
            foreach (var param in MethodOut)
            {
                param.CommandModel = this;
            }
            foreach (var param in ResultParameters)
            {
                param.CommandModel = this;
            }
        }

        #endregion

            #region Interface
        public new object Clone()
        {
            var clonedBase = (SatelliteMethodModel)base.Clone();
            return new SatelliteCommandModel(clonedBase)
            {
                _inputParameters = new List<List<object>>(this._inputParameters),
                _outputParameters = new List<List<object>>(this._outputParameters),
                _groupIndex = this._groupIndex,
                _groupName = this._groupName
            };
        }
        public override bool Equals(object? obj) => Equals(obj as SatelliteCommandModel);

        public bool Equals(SatelliteCommandModel? other)
        {
            if (other is null) return false;
            return File == other.File && Name == other.Name && GroupName == other.GroupName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(File, Name, GroupName);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _inputParameters.Clear();
                    _outputParameters.Clear();
                }


                _disposed = true;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
