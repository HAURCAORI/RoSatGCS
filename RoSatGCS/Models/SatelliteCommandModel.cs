using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePack;
using Newtonsoft.Json.Linq;
using NLog;
using OpenTK.Graphics.ES20;
using RoSatGCS.Controls;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Utils.Query;
using RoSatGCS.Utils.Timer;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Common.CommandTrees;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        public List<List<object>> InputParameters { get => _inputParameters; internal set => SetProperty(ref _inputParameters, value); }
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
            if (!IsValid)
            {
                MessageBoxResult result = await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(TranslationSource.Instance["zParametersInvalid"] + "\r\n" + TranslationSource.Instance["zExecuteAnyway"], TranslationSource.Instance["sInvalid"],
                MessageBoxButton.OKCancel, MessageBoxImage.Question));

                if (result != MessageBoxResult.OK)
                    return;
            }

            string byte_hex = string.Join(" ", InputSerialized.Select(b => b.ToString("X2")));
            string message = "Input Data:\r\n" + (byte_hex == string.Empty ? "(null)" : byte_hex);
            // Cancelable MessageBox
            var msgResult = await Application.Current.Dispatcher.InvokeAsync(() => {
             return MessageBox.Show(message, "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.None);
            });
            if (msgResult != MessageBoxResult.OK)
                return;


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

                    string dump = DumpResult(this).ReplaceLineEndings("");
                    dump = Regex.Replace(dump, @"\s+", " ");
                    Utils.Logger.Logger.LogInfo(dump);

                    const string output_folder = "CommandResults"; // Relative to application working directory
                    if (!Directory.Exists(output_folder))
                    {
                        Directory.CreateDirectory(output_folder);
                    }
                    string filename = $"{output_folder}/{DateTime.Now:yyyyMMdd_HHmmss}_{FIDLId}_{Id}.json";
                    await System.IO.File.WriteAllTextAsync(filename, DumpResult(this));
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


        public static string DumpResult(SatelliteCommandModel model)
        {
            var resultObject = new Dictionary<string, object?>();
            int index = 0;

            foreach (var param in model.MethodOut)
            {
                if (param.DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;

                var value = BuildValueForParameter(param, model, ref index);

                if (value == null)
                    continue;

                resultObject[param.Name] = value;
            }

            var root = new Dictionary<string, object?>
            {
                ["FIDL"] = model.FIDLId,
                ["Function"] = model.Name,      // or model.CommandName, etc.
                ["FunctionID"] = model.Id,
                ["Result"] = resultObject
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(root, options);
        }

        /// <summary>
        /// Build the value (primitive / array / struct / array-of-struct) for a single ParameterModel.
        /// This function mirrors the traversal logic of ConvertBack, but reads from OutputParameters
        /// instead of from raw bytes.
        /// </summary>
        private static object? BuildValueForParameter(ParameterModel param, SatelliteCommandModel model, ref int index)
        {
            var dataType = param.DataType;
            var baseType = param.BaseType;

            if (dataType == SatelliteFunctionFileModel.DataType.None)
                return null;

            // User-defined types (structs, enums, etc.)
            if (dataType == SatelliteFunctionFileModel.DataType.UserDefined)
            {
                if (baseType == SatelliteFunctionTypeModel.ArgumentType.Struct)
                {
                    if (!model.AssociatedType.TryGetValue(param.UserDefinedType, out var structType))
                        throw new InvalidDataException($"Unknown user-defined struct type '{param.UserDefinedType}'.");

                    if (param.IsArray)
                    {
                        // Array of structs:
                        // elementSize = sum of ByteSize of each field (one struct instance)
                        int elementSize = GetStructByteSize(structType);
                        int elementCount = elementSize == 0 ? 0 : param.ByteSize / elementSize;

                        var list = new List<object?>();
                        for (int i = 0; i < elementCount; i++)
                        {
                            var structValue = BuildStructValue(structType, model, ref index);
                            list.Add(structValue);
                        }

                        return list;
                    }
                    else
                    {
                        // Single struct
                        return BuildStructValue(structType, model, ref index);
                    }
                }
                else if (baseType == SatelliteFunctionTypeModel.ArgumentType.Enum)
                {
                    // ConvertBack uses:
                    // ret.Add(ParameterModel.ConvertValue(Enumeration, ...));
                    // -> one OutputParameters entry for the enum (possibly array)
                    var valueList = model.OutputParameters[index++];
                    if (!param.IsArray && valueList.Count == 1)
                        return valueList[0];

                    return valueList; // enum array (or multi-value) as list
                }
                else
                {
                    throw new NotImplementedException(
                        $"UserDefined type with base type {baseType} is not supported.");
                }
            }

            // Built-in types (integers, floats, bool, string, etc.)
            // ConvertBack always does: ret.Add(ParameterModel.ConvertValue(...));
            // so we just pick the next entry from OutputParameters.
            {
                var valueList = model.OutputParameters[index++];

                // Strings / primitives / enums: if not array and only one element, unwrap it.
                if (!param.IsArray && valueList.Count == 1)
                    return valueList[0];

                // Arrays (or multi-valued fields) -> keep as list
                return valueList;
            }
        }

        /// <summary>
        /// Build a dictionary representing a struct instance (hierarchical JSON object).
        /// </summary>
        private static Dictionary<string, object?> BuildStructValue(SatelliteFunctionTypeModel structType, SatelliteCommandModel model, ref int index)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var field in structType.Parameters)
            {
                if (field.DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;

                var value = BuildValueForParameter(field, model, ref index);

                if (value != null)
                    dict[field.Name] = value;
            }

            return dict;
        }

        /// <summary>
        /// Compute the byte size of a single instance of the given struct type.
        /// This matches the way ConvertBack consumes bytes for one struct:
        /// sum of ByteSize of all fields, including nested structs/arrays.
        /// </summary>
        private static int GetStructByteSize(SatelliteFunctionTypeModel structType)
        {
            int total = 0;

            foreach (var field in structType.Parameters)
            {
                // For each field, ByteSize already encodes its full size
                // (including an internal array or nested struct contents).
                total += field.ByteSize;
            }

            return total;
        }

        #endregion

        public static void ApplyDefaultInputValues(SatelliteCommandModel command)
        {
            // 1) Telemetry => setTelemetryPresetConfig
            if (command.FIDLId == 258 && command.Id == 3)
            {
                if (command.InSize == 151)
                {
                    const ushort default_data_id = 0xffff;
                    const byte default_active = 0;
                    const ushort default_period_ms = 1000;

                    List<List<object>> initial = [];
                    initial.Add(new List<object>() { (byte)0 });

                    for (int i = 0; i < 30; i++)
                    {
                        initial.Add(new List<object>() { default_data_id });
                        initial.Add(new List<object>() { default_active });
                        initial.Add(new List<object>() { default_period_ms });

                    }
                    command.InputParameters = initial;
                }
            }
            // 2) Beacon => set
            if (command.FIDLId == 257 && command.Id == 3)
            {
                if (command.InSize == 120)
                {
                    const ushort default_id = 0xffff;
                    List<List<object>> initial = [];

                    List<object> inside = [];
                    for(int i = 0; i < 60; i++)
                    {
                        inside.Add(default_id);
                    }
                    initial.Add(inside);

                    command.InputParameters = initial;
                }
            }
        }
    }
}
