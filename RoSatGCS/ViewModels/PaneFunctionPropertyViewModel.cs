using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using RoSatGCS.Models;
using RoSatGCS.Utils.Query;
using RoSatGCS.Utils.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;


namespace RoSatGCS.ViewModels
{
    public class PaneFunctionPropertyViewModel : PaneViewModel, IDisposable
    {
        #region Fields
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly WeakReference<PageCommandViewModel> _parent;
        public PageCommandViewModel? Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out var target))
                    return target;
                return null;
            }
        }

        private bool _isModified = false;
        private bool _isParametersVisible = true;
        private bool _isResultsVisible = true;
        private SatelliteCommandModel? _command;
        private ObservableCollection<ParameterModel> _inputParameters = [];
        private ObservableCollection<ParameterModel> _outParameters = [];
        private RelayCommand? _save;
        private RelayCommand? _execute;
        #endregion

        #region Parameters
        public bool IsModified { get => _isModified; set { SetProperty(ref _isModified, value); UpdateTitle(); } }
        public bool IsParametersVisible { get => HasInput && _isParametersVisible; set => SetProperty(ref _isParametersVisible, value); }
        public bool IsResultsVisible { get => HasOutput && _isResultsVisible; set => SetProperty(ref _isResultsVisible, value); }
        public bool HasInput { get => Command?.MethodIn.Count > 0; }
        public bool HasOutput { get => Command?.MethodOut.Count > 0; }
        public SatelliteCommandModel? Command { get { return _command; } }

        public ObservableCollection<ParameterModel> InputParameters { get => _inputParameters; set { SetProperty(ref _inputParameters, value);  } }
        public ObservableCollection<ParameterModel> OutputParameters { get => _outParameters; set { SetProperty(ref _outParameters, value); } }
        #endregion

        #region Command
        public ICommand Close { get; set; }
        public ICommand ParametersClick { get; set; }
        public ICommand ResultsClick { get; set; }
        public ICommand Save { get { return _save ??= new RelayCommand(OnSave, CanSave); } }
        public ICommand Execute { get { return _execute ??= new RelayCommand(OnExecute, CanExecute); } }

        #endregion


        public PaneFunctionPropertyViewModel(PageCommandViewModel viewModel, SatelliteCommandModel command)
        {
            _parent = new WeakReference<PageCommandViewModel>(viewModel);
            _command = command;
            _command.Received += OnReceived;

            UpdateTitle();

            Application.Current.Dispatcher.Invoke(() => { InitializeMethod(); InitializeValues(); });  
            
            Close = new RelayCommand(OnClose);
            ParametersClick = new RelayCommand(OnParametersClick);
            ResultsClick = new RelayCommand(OnResultsClick);
        }

        #region Implementations
        private void OnClose()
        {
            if (Parent == null)
                return;
            if (Command != null && Command.IsTemp)
                Parent.RemoveTempCommand.Execute(Command);
            Parent.CloseDocument(this);
            
            foreach(var input in InputParameters)
            {
                input.Dispose();
            }

            foreach (var output in OutputParameters)
            {
                output.Dispose();
            }

            this.Dispose();
        }

        private void OnParametersClick()
        {
            IsParametersVisible = !IsParametersVisible;
        }

        private void OnResultsClick()
        {
            IsResultsVisible = !IsResultsVisible;
        }

        private void OnSave()
        {
            if(Parent == null) { return; }
            if(Command == null) { return; }

            // Save Praameters
            var temp = Command.InputParameters;
            Command.InputParameters.Clear();
            foreach (var input in InputParameters)
            {
                if (input.DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;
                input.PullValue?.Execute(null);
                if (input.DataType == SatelliteFunctionFileModel.DataType.String)
                {
                    var val = input.Value ?? [];
                    if (val.Count == 1)
                    {
                        if(val.First() is string sval)
                        {
                            Command.InputParameters.Add([ToFixedSizeCString(sval, input.ByteSize)]);
                        }
                        else
                        {
                            Command.InputParameters.Add(val);
                        }
                    }
                    else
                    {
                        Command.InputParameters.Add(val);
                    }
                }
                else
                {
                    Command.InputParameters.Add(input.Value ?? []);
                }
            }

            IsModified = false;
            _save?.NotifyCanExecuteChanged();

            Command.IsValid = CanExecute();


            if(Command.InSize != Command.InputSize)
            {
                MessageBoxResult result = Application.Current.Dispatcher.Invoke(() => MessageBox.Show(TranslationSource.Instance["zSaveAnyway"], TranslationSource.Instance["zInvalidSize"]
                    , MessageBoxButton.OKCancel, MessageBoxImage.Information));
                if (result != MessageBoxResult.OK)
                {
                    Command.InputParameters.Clear();
                    Command.InputParameters = temp;
                    return;
                }
            }

            // Serialize Parameters
            try
            {
                Command.InputSerialized = QueryExecutorBase.Serializer(Command.InputParameters);
            }
            catch (NotSupportedException)
            {
                Logger.Error("Error while serializing parameters:" + Command.Name);
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(TranslationSource.Instance["zSerializeError"], TranslationSource.Instance["sError"]
                    , MessageBoxButton.OK, MessageBoxImage.Error));
            }


            // Add to Group
            if (Command.GroupName == null)
            {
                string? groupName = null;
                if (Parent.PaneFunctionList?.SelectedCommandGroup != null)
                {
                    groupName = Parent.PaneFunctionList?.SelectedCommandGroup.Name;
                }
                Command.GroupName = groupName;
                Parent.AddCommand.Execute(Command);
            }
        }

        private bool CanSave()
        {
            return Command?.GroupName == null || Command?.GroupName == string.Empty || IsModified;
        }

        private bool CanExecute()
        {
            foreach(var i in InputParameters)
            {
                if (i.DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;
                if (i.BaseType != SatelliteFunctionTypeModel.ArgumentType.Enum && i.DataType != SatelliteFunctionFileModel.DataType.Boolean && i.HasError)
                    return false;
            }
            return true;
        }

        private void OnExecute()
        {
            if(Command == null) { return; }

            List<List<object>> parameters = [];

            foreach(var input in InputParameters)
            {
                if (input.DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;
                input.PullValue?.Execute(null);
                var list = input.Value;
                if (list == null)
                    return;
                List<object> values = [];


                foreach (var value in list)
                {
                    if (input.DataType == SatelliteFunctionFileModel.DataType.String)
                    {
                        if (value is string sval)
                        {
                            values.Add(ToFixedSizeCString(sval, input.ByteSize));
                        }
                        else
                        {
                            values.Add(value);
                        }
                    }
                    else
                    {
                        values.Add(value);
                    }
                }

                parameters.Add(values);
            }

            Command.IsValid = CanExecute();

            try
            {
                Command.InputSerialized = QueryExecutorBase.Serializer(parameters);
                Command.Execute.Execute(null);
            }
            catch(NotSupportedException)
            {
                Logger.Error("Error while serializing parameters:" + Command.Name);
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(TranslationSource.Instance["zSerializeError"], TranslationSource.Instance["sError"]
                    , MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void OnReceived(object? sender, EventArgs e)
        {
            if (Command == null) return;

            for (int i = 0, j = 0; i < OutputParameters.Count; i++)
            {
                if (OutputParameters[i].DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;
                if (j < Command.OutputParameters.Count)
                    OutputParameters[i].Value = Command.OutputParameters[j++];
                OutputParameters[i].ReceivedEvent(this);
            }
        }

        #endregion

        #region Functions
        private void InitializeValues()
        {
            if (Command == null) return;

            for (int i = 0, j = 0; i < InputParameters.Count; i++)
            {
                if (InputParameters[i].DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;
                if (j < Command.InputParameters.Count)
                    InputParameters[i].Value = Command.InputParameters[j++];
            }

            // To accomdate the last parameter which the size is variable
            if (InputParameters.Count > 0)
            {
                InputParameters.Last().IsLast = true;
            }

            for (int i = 0, j = 0; i < OutputParameters.Count; i++)
            {
                if (OutputParameters[i].DataType == SatelliteFunctionFileModel.DataType.None)
                    continue;
                if (j < Command.OutputParameters.Count)
                    OutputParameters[i].Value = Command.OutputParameters[j++];
            }
        }

        private void InitializeMethod()
        {
            if (Command == null) return; 
            var sequence = 1;
            foreach (var param in Command.MethodIn)
            {
                param.CommandModel = Command;
                param.IsReadOnly = false;
                param.Sequence = (sequence++).ToString();

                
                foreach (var t in ParameterModel.GetType(param))
                {
                    t.ValueChanged += (sender, e) => _execute?.NotifyCanExecuteChanged();
                    t.ValueChanged += (sender, e) => IsModified = true;
                    t.ValueChanged += (sender, e) => _save?.NotifyCanExecuteChanged();
                    InputParameters.Add(t);
                }
            }

            sequence = 1;
            foreach (var param in Command.MethodOut)
            {
                param.CommandModel = Command;
                param.IsReadOnly = true;
                param.Sequence = (sequence++).ToString();

                foreach (var t in ParameterModel.GetType(param))
                {
                    t.ValueChanged += (sender, e) => IsModified = true;
                    t.ValueChanged += (sender, e) => _save?.NotifyCanExecuteChanged();
                    OutputParameters.Add(t);
                }
            }
        }


        public void UpdateTitle()
        {
            if(Command == null) { return; }
            string name = Command.Name + ((Command.GroupName != null && Command.GroupName != string.Empty) ? $"[{Command.GroupName}]" : "");
            Title = IsModified ? $"{name} *" : name;
        }

        static string ToFixedSizeCString(string input, int size)
        {
            byte[] strBytes = Encoding.ASCII.GetBytes(input);
            byte[] buffer = new byte[size];

            int copyLen = Math.Min(strBytes.Length, size - 1); // reserve room for null
            Array.Copy(strBytes, 0, buffer, 0, copyLen);
            buffer[copyLen] = 0x00; // null-terminator

            return Encoding.ASCII.GetString(buffer);
        }

        #endregion

        #region Dispose

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                if (Command != null)
                {
                    foreach (var param in Command.MethodIn)
                    {
                        foreach (var t in ParameterModel.GetType(param))
                        {
                            t.ValueChanged -= (sender, e) => _execute?.NotifyCanExecuteChanged();
                            t.ValueChanged -= (sender, e) => IsModified = true;
                            t.ValueChanged -= (sender, e) => _save?.NotifyCanExecuteChanged();
                            
                        }
                    }
                    foreach (var param in Command.MethodOut)
                    {
                        foreach (var t in ParameterModel.GetType(param))
                        {
                            t.ValueChanged -= (sender, e) => IsModified = true;
                            t.ValueChanged -= (sender, e) => _save?.NotifyCanExecuteChanged();
                        }
                    }
                }

                _inputParameters.Clear();
                _outParameters.Clear();

                
                if (_command != null)
                {
                    _command.Received -= OnReceived;
                }
                _command = null;

            }
            _disposed = true;
        }
        #endregion
    }
}
