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

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class SatelliteCommandModel : SatelliteMethodModel, ICloneable, IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool _isMessageBoxShown = false;

        public class ReceivedEventArgs : EventArgs
        {
            public required List<byte> Data { get; set; }
        }

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
        #endregion

        #region Properties
        [IgnoreMember]
        public List<byte> InputSerialized { get => _inputSerialized; internal set => _inputSerialized = value; }
        [IgnoreMember]
        public List<List<object>> InputParameters { get => _inputParameters; internal set => _inputParameters = value; }
        [IgnoreMember]
        public List<List<object>> OutputParameters { get => _outputParameters; internal set => _outputParameters = value; }
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

        #endregion

        #region Commands
        [IgnoreMember]
        private RelayCommand? _execute;
        [IgnoreMember]
        public ICommand Execute { get => _execute ??= new RelayCommand(OnExecute); }
        #endregion

        #region Constructors
        private SatelliteCommandModel() : base() { }
        public SatelliteCommandModel(SatelliteMethodModel method) : base(method.Id, method.Visibility, method.File, method.Name, method.Description)
        {
            var copy = (SatelliteMethodModel) method.Clone();
            _methodIn = new List<ParameterModel>(copy.MethodIn);
            _methodOut = new List<ParameterModel>(copy.MethodOut);
            _associatedType = new Dictionary<string, SatelliteFunctionTypeModel>(copy.AssociatedType);
            if (_methodIn.Count() == 0)
                IsValid = true;
        }


        #endregion

        #region Implementations

        private List<List<object>> ConvertBack(List<byte> Serialized)
        {
            List<List<object>> ret = [];
            foreach (var output in MethodOut)
            {
                if (output.DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;

                if (output.DataType == SatelliteFunctionFileModel.DataType.UserDefined)
                {
                    if (output.BaseType == SatelliteFunctionTypeModel.ArgumentType.Struct)
                    {
                        AssociatedType.TryGetValue(output.UserDefinedType, out SatelliteFunctionTypeModel? tmp);
                        if (tmp is null)
                        {
                            throw new InvalidDataException();
                        }
                        int j = 0;

                        tmp.Parameters.ForEach(p =>
                        {
                            if ((j + p.ByteSize) > Serialized.Count) { return; }
                            ret.Add(ParameterModel.ConvertValue(p.DataType, Serialized.GetRange(j, p.ByteSize), p.BaseType));
                            j += p.ByteSize;
                        });
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    ret.Add(ParameterModel.ConvertValue(output.DataType, Serialized.GetRange(0, output.ByteSize)));
                }
            }
            return ret;
        }

        private async void OnExecute()
        {
            IsExecuting = true;
            try
            {
                var ret = await ZeroMqQueryExecutor.Instance.ExecuteAsync(this, DispatcherType.Postpone);
                if (ret is byte[] b)
                {
                    OutputParameters = ConvertBack([.. b]);
                    Received?.Invoke(this, new EventArgs());
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
