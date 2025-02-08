using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using RoSatGCS.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using RoSatGCS.Utils.Localization;
using MessagePack;
using System.IO;
using System.Windows.Data;
using AvalonDock;
using AvalonDock.Layout;
using System.ComponentModel;

namespace RoSatGCS.ViewModels
{
       
    public class PageCommandViewModel : ViewModelPageBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static string PathCacheFuncFile = "Cache/func_file_list";

        #region Fields
        private bool initialized = false;
        private ObservableCollection<PaneViewModel> _anchorable = new ObservableCollection<PaneViewModel>();
        private ObservableCollection<PaneViewModel> _document = new ObservableCollection<PaneViewModel>();
        private PaneCommandFileViewModel _paneCommandFile;
        private PaneTypeDictionaryViewModel _paneTypeDictionary;
        private PaneFunctionListViewModel _paneFunctionList;
        private PaneCommandSetViewModel _paneCommandSet;
        private PaneViewModel? _activeDocument;
        #endregion

        #region Properties

        public ObservableCollection<PaneViewModel> Anchorable { get => _anchorable; }
        public ObservableCollection<PaneViewModel> DocumentPane { get => _document; }
        public PaneCommandFileViewModel PaneCommandFile { get => _paneCommandFile; }
        public PaneTypeDictionaryViewModel PaneTypeDictionary { get => _paneTypeDictionary; }
        public PaneFunctionListViewModel PaneFunctionList { get => _paneFunctionList; }

        // Sat Func File
        private ObservableCollection<SatelliteFunctionFileModel> _satFuncFile = new ObservableCollection<SatelliteFunctionFileModel>();
        public ReadOnlyObservableCollection<SatelliteFunctionFileModel> SatFuncFile { get; private set; }

        private ListCollectionView _satFuncFileView;
        public ListCollectionView SatelliteFunctionFileView { get => _satFuncFileView; }

        // Sat Func Type
        private ObservableCollection<SatelliteFunctionTypeModel> _satFuncType = new ObservableCollection<SatelliteFunctionTypeModel>();
        public ObservableCollection<SatelliteFunctionTypeModel> SatelliteFunctionTypes { get => _satFuncType; }

        private ListCollectionView _satFuncTypeView;
        public ListCollectionView SatelliteFunctionTypesView { get => _satFuncTypeView; }

        // Sat Method
        private ObservableCollection<SatelliteMethodModel> _satMethod = new ObservableCollection<SatelliteMethodModel>();
        public ObservableCollection<SatelliteMethodModel> SatelliteMethod { get => _satMethod; }

        private ListCollectionView _satMethodView;
        public ListCollectionView SatelliteMethodView { get => _satMethodView; }


        public PaneViewModel? ActiveDocument
        {
            get => _activeDocument;
            set
            {
                if (_activeDocument != value)
                {
                    SetProperty(ref _activeDocument, value);
                }
            }
        }
        #endregion

        #region Command
        public ICommand Loaded { get; }
        public ICommand Closing { get; }
        public ICommand AddSatFuncFile { get; }
        public ICommand RemoveSatFuncFile { get; }
        public ICommand Refresh { get; }
        public ICommand RefreshAll { get; }
        public ICommand TypeHyperLinkClick { get; }
        public ICommand VisibilitySwap { get; }
        #endregion

        #region Constructor
        public PageCommandViewModel()
        {
            _satFuncFileView = new ListCollectionView(_satFuncFile);
            _satFuncFileView.SortDescriptions.Add(new SortDescription(nameof(SatelliteFunctionFileModel.Name), ListSortDirection.Ascending));
            _satFuncTypeView = new ListCollectionView(_satFuncType);
            _satMethodView = new ListCollectionView(_satMethod);


            Loaded = new RelayCommand(OnLoaded);
            Closing = new RelayCommand(OnClosing);
            AddSatFuncFile = new RelayCommand<SatelliteFunctionFileModel>(OnAddSatFuncFile);
            RemoveSatFuncFile = new RelayCommand<SatelliteFunctionFileModel>(OnRemoveSatFuncFile);
            Refresh = new RelayCommand<SatelliteFunctionFileModel>(OnRefresh);
            RefreshAll = new RelayCommand(OnRefreshAll);
            TypeHyperLinkClick = new RelayCommand<object>(OnTypeHyperLinkClick);
            VisibilitySwap = new RelayCommand<object>(OnVisibilitySwap);

            SatFuncFile = new ReadOnlyObservableCollection<SatelliteFunctionFileModel>(_satFuncFile);

            _paneCommandFile = new PaneCommandFileViewModel(this);
            _anchorable.Add(_paneCommandFile);

            _paneTypeDictionary = new PaneTypeDictionaryViewModel(this);
            _anchorable.Add(_paneTypeDictionary);

            _paneFunctionList = new PaneFunctionListViewModel(this);
            _document.Add(_paneFunctionList);

            _paneCommandSet = new PaneCommandSetViewModel(this);
            _document.Add(_paneCommandSet);
        }
        #endregion


        #region Command Implementation
        private void OnLoaded()
        {
            if(initialized) { return; }

            if (!File.Exists(PathCacheFuncFile))
            {
                return;
            }
            
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

            RefreshAll.Execute(null);
            initialized = true;
        }

        private void OnClosing()
        {
            Directory.CreateDirectory("Cache");
            using (var fileStream = new FileStream(PathCacheFuncFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                MessagePackSerializer.Serialize(fileStream, _satFuncFile);
            }
        }

        private void OnAddSatFuncFile(SatelliteFunctionFileModel? model)
        {
            if(model != null)
            {
                _satFuncFile.Add(model);
                
                Refresh.Execute(model);
            }
        }

        private void OnRemoveSatFuncFile(SatelliteFunctionFileModel? model)
        {
            if (model != null)
            {
                // Remove all related type
                var e_type = SatelliteFunctionTypes.Where(t => t.File == model.Name).ToList();
                foreach (var t in e_type)
                    _satFuncType.Remove(t);

                // Remove all related method
                var e_method = SatelliteMethod.Where(t => t.File == model.Name).ToList();
                foreach (var t in e_method)
                    _satMethod.Remove(t);

                // Remove function file
                _satFuncFile.Remove(model);
            }
        }

        private void OnRefresh(SatelliteFunctionFileModel? model)
        {
            if (model == null) { return; }

            bool error = false;
            model.Valid = false;

            var file = SatFuncFile.FirstOrDefault(t => t.Name == model.Name);
            if (file == null) { return; }
            if (file.Structure == null) { return; }

            // Remove all related type
            var e_type = SatelliteFunctionTypes.Where(t => t.File == model.Name).ToList();
            foreach (var t in e_type)
                _satFuncType.Remove(t);

            // Remove all related method
            var e_method = SatelliteMethod.Where(t => t.File == model.Name).ToList();
            foreach (var t in e_method)
                _satMethod.Remove(t);

            var st = file.Structure.Value;

            List<SatelliteFunctionTypeModel> tempSatFuncType = new List<SatelliteFunctionTypeModel>();
            List<SatelliteMethodModel> tempSatMethod = new List<SatelliteMethodModel>();


            // Initialize Enumeration
            foreach (var el in st.Enumeration)
            {
                var ret = new SatelliteFunctionTypeModel(SatelliteFunctionTypeModel.ArgumentType.Enum, file.Name, el.Name, el.Description)
                {
                    Visibility = file.Visibility,
                    Size = el.Values.Count
                };

                foreach (var e in el.Values)
                {
                    var par = new ParameterModel(SatelliteFunctionTypeModel.ArgumentType.Enum, file.Name, el.Name, el.Description);
                    par.Id = e.Id;
                    ret.Parameters.Add(par);
                }

                tempSatFuncType.Add(ret);
            }

            // Initialize the struct
            foreach (var el in st.Struct)
            {
                var ret = new SatelliteFunctionTypeModel(SatelliteFunctionTypeModel.ArgumentType.Struct, file.Name, el.Name, el.Description)
                {
                    Visibility = file.Visibility,
                    Size = el.Size
                };

                int index = 0;
                foreach (var e in el.Values)
                {
                    var par = new ParameterModel(SatelliteFunctionTypeModel.ArgumentType.Struct, file.Name, el.Name, el.Description);
                    par.ByteSize = e.Size;
                    par.IsArray = e.Is_Array;
                    par.Index = index++;

                    (par.DataType, par.UserDefinedType) = TypeParseHelper(e.Type);

                    ret.Parameters.Add(par);
                }

                tempSatFuncType.Add(ret);
            }

            // Initialize the method
            foreach (var el in st.Method)
            {
                var ret = new SatelliteMethodModel(el.Id, file.Visibility, file.Name, el.Name, el.Description);

                int index = 0;

                ParameterModel CreateParameterModel(SatelliteFunctionFileModel.MethodValue e)
                {
                    var par = new ParameterModel(SatelliteFunctionTypeModel.ArgumentType.None, file.Name, e.Name, e.Description)
                    {
                        ByteSize = e.Size,
                        IsArray = e.Is_Array,
                        Index = index++
                    };

                    (par.DataType, par.UserDefinedType) = TypeParseHelper(e.Type);

                    if (par.IsUserDefined)
                    {
                        var type = SearchType(tempSatFuncType, par.File, par.UserDefinedType);
                        
                        if (type == null)
                        {
                            Logger.Error("Type not found");

                            Application.Current.Dispatcher.Invoke(() =>  MessageBox.Show(TranslationSource.Instance["zTypeNotFound"] + ": " + par.UserDefinedType, model.Name));
                            error = true;
                        }
                        else
                        {
                            ret.AssociatedType.Add(type.Name, (SatelliteFunctionTypeModel) type.Clone());
                        }
                    }

                    return par;
                }

                // Add Method In Parameters
                ret.MethodIn.AddRange(el.In.Select(CreateParameterModel));

                // Add Method Out Parameters
                index = 0;
                ret.MethodOut.AddRange(el.Out.Select(CreateParameterModel));


                tempSatMethod.Add(ret);
            }


            model.Valid = !error;
            if (!error)
            {
                foreach (var i in tempSatFuncType)
                    _satFuncType.Add(i);
                foreach (var i in tempSatMethod)
                    _satMethod.Add(i);
            }

            PaneTypeDictionary.ApplyFilter.Execute(null);
            PaneFunctionList.ApplyFilter.Execute(null);

#if DEBUG
            // Dump types
            var commands = tempSatMethod.Select(p => p.GetCommandModel());
            Directory.CreateDirectory("Cache");
            using (var fileStream = new FileStream("Cache/method", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                MessagePackSerializer.Serialize(fileStream, commands);
            }

#endif
        }

        private void OnRefreshAll()
        {
            _satFuncType.Clear();
            foreach (var item in _satFuncFile)
            {
                Refresh.Execute(item);
            }
        }
        
        private void OnTypeHyperLinkClick(object? sender)
        {
            if (sender is ParameterModel param)
            {
                if (param.Name == "" || !param.IsUserDefined) { return; }

                var search = SearchType(_satFuncType.ToList(), param.File, param.UserDefinedType);
                if (search == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show(TranslationSource.Instance["zTypeNotFound"] +": "+ param.UserDefinedType, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error));
                    Logger.Error("Type not found: " + param.UserDefinedType);
                    return;
                }

                var pane = new PaneTypeSummaryViewModel(this);
                pane.Title = search.Name;
                pane.id = (search.Name + search.File).GetHashCode();
                pane.SatFuncType = search;

                var item = DocumentPane.FirstOrDefault(x => x.id == pane.id);
                if (item == null)
                {
                    DocumentPane.Add(pane);
                }
                else
                {
                    ActiveDocument = item;
                }
            }
        }

        private void OnVisibilitySwap(object? obj)
        {
            if (obj is SatelliteFunctionFileModel model)
            {
                // Remove all related type
                var e_type = SatelliteFunctionTypes.Where(t => t.File == model.Name).ToList();
                foreach (var t in e_type)
                    t.Visibility = model.Visibility;
                PaneTypeDictionary.ApplyFilter.Execute(null);

                // Remove all related method
                var e_method = SatelliteMethod.Where(t => t.File == model.Name).ToList();
                foreach (var t in e_method)
                    t.Visibility = model.Visibility;
                PaneFunctionList.ApplyFilter.Execute(null);
            }
        }
        #endregion

        #region Functions

        public static SatelliteFunctionTypeModel? SearchType(List<SatelliteFunctionTypeModel> types, string file, string name)
        {
            var ret = types.Where((o) => o.File == file && o.Name == name );
            if (ret.Count() > 1)
            {
                Logger.Error("Multiple Type Definitions");
                return null;
            }

            return ret.FirstOrDefault();
        }

        private static (SatelliteFunctionFileModel.DataType type, string str) TypeParseHelper(string str)
        {
            SatelliteFunctionFileModel.DataType parse;
            bool success = Enum.TryParse(str, ignoreCase: false, result: out parse);
            if (!success)
            {
                var sp = str.Trim().Split('|');

                if (sp.Length != 2 || sp[0] != "UserDefined" || sp[1].Length == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show(TranslationSource.Instance["zStructEnumParseError"] + ":" + str, TranslationSource.Instance["zParseError"], MessageBoxButton.OK, MessageBoxImage.Error));
                    Logger.Error("Struct Type Error");
                }

                parse = SatelliteFunctionFileModel.DataType.UserDefined;
                return (parse, sp[1]);
            }
            return (parse, "");
        }

        internal void CloseDocument(PaneViewModel document)
        {
            _document.Remove(document);
        }
        #endregion
    }
}
