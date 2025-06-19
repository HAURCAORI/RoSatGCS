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
using static RoSatGCS.Models.SatelliteFunctionFileModel;
using RoSatGCS.Controls;
using System.Security.Policy;
using NLog.Layouts;
using System.Windows.Threading;

namespace RoSatGCS.ViewModels
{
       
    public class PageCommandViewModel : ViewModelPageBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly string PathCacheFuncFile = "Cache/func_file_list";
        private static readonly string PathCacheCommandFile = "Cache/command_list";

        #region Fields
        private bool initialized = false;
        private readonly ObservableCollection<PaneViewModel> _anchorable = [];
        private readonly ObservableCollection<PaneViewModel> _document = [];
        private readonly WeakReference<PaneCommandFileViewModel> _paneCommandFile;
        private readonly WeakReference<PaneTypeDictionaryViewModel> _paneTypeDictionary;
        private readonly WeakReference<PaneFunctionListViewModel> _paneFunctionList;
        private readonly WeakReference<PaneCommandSetViewModel> _paneCommandSet;
        private readonly PanePropertyPreviewViewModel _panePropertyPreview;
        private PaneViewModel? _activeDocument;
        #endregion

        #region Properties

        public ObservableCollection<PaneViewModel> Anchorable { get => _anchorable; }
        public ObservableCollection<PaneViewModel> DocumentPane { get => _document; }


        public PaneCommandFileViewModel? PaneCommandFile { get => _paneCommandFile?.TryGetTarget(out var target) == true ? target : null; }
        public PaneTypeDictionaryViewModel? PaneTypeDictionary { get => _paneTypeDictionary?.TryGetTarget(out var target) == true ? target : null; }
        public PaneFunctionListViewModel? PaneFunctionList { get => _paneFunctionList?.TryGetTarget(out var target) == true ? target : null; }
        public PaneCommandSetViewModel? PaneCommandSet { get => _paneCommandSet?.TryGetTarget(out var target) == true ? target : null; }
        public PanePropertyPreviewViewModel PanePropertyPreview { get => _panePropertyPreview; }

        // Sat Func File
        private ObservableCollection<SatelliteFunctionFileModel> _satFuncFile = [];
        public ReadOnlyObservableCollection<SatelliteFunctionFileModel> SatFuncFile { get; private set; }

        private ListCollectionView _satFuncFileView;
        public ListCollectionView SatelliteFunctionFileView { get => _satFuncFileView; }

        // Sat Func Type
        private readonly ObservableCollection<SatelliteFunctionTypeModel> _satFuncType = [];
        public ObservableCollection<SatelliteFunctionTypeModel> SatelliteFunctionTypes { get => _satFuncType; }

        private readonly ListCollectionView _satFuncTypeView;
        public ListCollectionView SatelliteFunctionTypesView { get => _satFuncTypeView; }

        // Sat Method
        private readonly ObservableCollection<SatelliteMethodModel> _satMethod = [];
        public ObservableCollection<SatelliteMethodModel> SatelliteMethod { get => _satMethod; }

        private readonly ListCollectionView _satMethodView;
        public ListCollectionView SatelliteMethodView { get => _satMethodView; }

        // Sat Command

        private ObservableCollection<SatelliteCommandGroupModel> _satCommandGroup = [];
        public ReadOnlyObservableCollection<SatelliteCommandGroupModel> SatelliteCommandGroup { get; private set; }

        private ListCollectionView _satCommandGroupView;
        public ListCollectionView SatelliteCommandGroupView { get => _satCommandGroupView; }

        // Temp
        private readonly List<SatelliteCommandModel> _satCommandTemp = [];
        

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
        public ICommand AddCommand { get; }
        public ICommand RemoveTempCommand { get; }
        public ICommand GroupAdd { get; }
        #endregion

        #region Constructor
        public PageCommandViewModel()
        {
            _satFuncFileView = new ListCollectionView(_satFuncFile);
            _satFuncFileView.SortDescriptions.Add(new SortDescription(nameof(SatelliteFunctionFileModel.Name), ListSortDirection.Ascending));
            _satFuncTypeView = new ListCollectionView(_satFuncType);
            _satMethodView = new ListCollectionView(_satMethod);
            _satCommandGroupView = new ListCollectionView(_satCommandGroup);

            Loaded = new RelayCommand(OnLoaded);
            Closing = new RelayCommand(OnClosing);
            AddSatFuncFile = new RelayCommand<SatelliteFunctionFileModel>(OnAddSatFuncFile);
            RemoveSatFuncFile = new RelayCommand<SatelliteFunctionFileModel>(OnRemoveSatFuncFile);
            Refresh = new RelayCommand<SatelliteFunctionFileModel>(OnRefresh);
            RefreshAll = new RelayCommand(OnRefreshAll);
            TypeHyperLinkClick = new RelayCommand<object>(OnTypeHyperLinkClick);
            VisibilitySwap = new RelayCommand<object>(OnVisibilitySwap);
            AddCommand = new RelayCommand<SatelliteCommandModel>(OnAddCommand);
            RemoveTempCommand = new RelayCommand<SatelliteCommandModel>(OnRemoveTempCommand);
            GroupAdd = new RelayCommand<SatelliteCommandGroupModel>(OnGroupAdd);

            SatFuncFile = new ReadOnlyObservableCollection<SatelliteFunctionFileModel>(_satFuncFile);
            SatelliteCommandGroup = new ReadOnlyObservableCollection<SatelliteCommandGroupModel>(_satCommandGroup);

            _paneCommandFile = new WeakReference<PaneCommandFileViewModel>(new PaneCommandFileViewModel(this));
            if (PaneCommandFile != null)
                _anchorable.Add(PaneCommandFile);

            _paneTypeDictionary = new WeakReference<PaneTypeDictionaryViewModel>(new PaneTypeDictionaryViewModel(this));
            if (PaneTypeDictionary != null)
                _anchorable.Add(PaneTypeDictionary);

            _paneFunctionList = new WeakReference<PaneFunctionListViewModel>(new PaneFunctionListViewModel(this));
            if (PaneFunctionList != null)
                _document.Add(PaneFunctionList);

            _paneCommandSet = new WeakReference<PaneCommandSetViewModel>(new PaneCommandSetViewModel(this));
            if (PaneCommandSet != null)
                _document.Add(PaneCommandSet);

            _panePropertyPreview = new PanePropertyPreviewViewModel(this);
        }
        #endregion


        #region Command Implementation
        private void OnLoaded()
        {
            if(initialized) { return; }
     
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
                            _satCommandGroup.Add(new SatelliteCommandGroupModel(this, item));
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

            
            RefreshAll.Execute(null);
            initialized = true;
        }

        private void OnClosing()
        {
            if (!initialized) { return; }
            Directory.CreateDirectory("Cache");
            using var fileStream = new FileStream(PathCacheFuncFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            MessagePackSerializer.Serialize(fileStream, _satFuncFile);
            using var commandStream = new FileStream(PathCacheCommandFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            MessagePackSerializer.Serialize(commandStream, _satCommandGroup);
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

            List<SatelliteFunctionTypeModel> tempSatFuncType = [];
            List<SatelliteMethodModel> tempSatMethod = [];


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
                    var par = new ParameterModel(SatelliteFunctionTypeModel.ArgumentType.Enum, file.Name, e.Name, e.Description)
                    {
                        Id = e.Id
                    };
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

                    var par = new ParameterModel(SatelliteFunctionTypeModel.ArgumentType.Struct, file.Name, e.Name, e.Description)
                    {
                        ByteSize = e.Size,
                        IsArray = e.Is_Array,
                        Index = index++
                    };

                    (par.DataType, par.UserDefinedType) = TypeParseHelper(e.Type);

                    if(par.IsUserDefined)
                    {
                        var type = SearchType(tempSatFuncType, par.File, par.UserDefinedType);
                        if (type == null)
                        {
                            Logger.Error("Type not found");

                            Application.Current.Dispatcher.Invoke(() => MessageBox.Show(TranslationSource.Instance["zTypeNotFound"] + ": " + par.UserDefinedType, model.Name));
                            error = true;
                        }
                        else
                        {
                            par.BaseType = type.Type;
                        }
                    }

                    ret.Parameters.Add(par);
                }

                tempSatFuncType.Add(ret);
            }

            // Initialize the method
            foreach (var el in st.Method)
            {
                var ret = new SatelliteMethodModel(el.Id, file.Visibility, file.Name, el.Name, el.Description, st.Id);
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
                        var type = SearchAssociatedType(tempSatFuncType, par.File, par.UserDefinedType);
                        
                        if (type.Count == 0)
                        {
                            Logger.Error("Type not found");

                            Application.Current.Dispatcher.Invoke(() =>  MessageBox.Show(TranslationSource.Instance["zTypeNotFound"] + ": " + par.UserDefinedType, model.Name));
                            error = true;
                        }
                        else
                        {
                            foreach(var item in type)
                            {
                                ret.AssociatedType.TryAdd(item.Name, (SatelliteFunctionTypeModel)item.Clone());
                            }
                            par.BaseType = type[0].Type;
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
            
            PaneTypeDictionary?.ApplyFilter.Execute(null);
            PaneFunctionList?.ApplyFilter.Execute(null);
            /*
#if DEBUG
            
            // Dump types
            var commands = tempSatMethod.Select(p => p.GetCommandModel());
            Directory.CreateDirectory("Cache");
            using (var fileStream = new FileStream("Cache/method", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                MessagePackSerializer.Serialize(fileStream, commands);
            }
           
#endif
             */
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

                var search = SearchType([.. _satFuncType], param.File, param.UserDefinedType);
                if (search == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show(TranslationSource.Instance["zTypeNotFound"] +": "+ param.UserDefinedType, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error));
                    Logger.Error("Type not found: " + param.UserDefinedType);
                    return;
                }

                var pane = new PaneTypeSummaryViewModel(this)
                {
                    Title = search.Name,
                    id = (search.Name + search.File).GetHashCode(),
                    SatFuncType = search
                };

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
                PaneTypeDictionary?.ApplyFilter.Execute(null);

                // Remove all related method
                var e_method = SatelliteMethod.Where(t => t.File == model.Name).ToList();
                foreach (var t in e_method)
                    t.Visibility = model.Visibility;
                PaneFunctionList?.ApplyFilter.Execute(null);
            }
        }

        private void OnAddCommand(SatelliteCommandModel? o)
        {
            if (o == null) { return; }
            o.GroupName ??= "Default";
            if (o.GroupName.Replace(" ","") == string.Empty)
                o.GroupName ??= "Default";

            FindFunctionPropertyPane(o)?.UpdateTitle();

            var item = SatelliteCommandGroup.FirstOrDefault(g => g.Name == o.GroupName);
            if(item == null)
            {
                item = new SatelliteCommandGroupModel(this, o.GroupName);
                _satCommandGroup.Add(item);
            }

            if (o.IsTemp == true)
            {
                var t = _satCommandTemp.Find(t => t.Equals(o));
                if (t != null)
                {
                    item.Add(t, true);
                    _satCommandTemp.Remove(t);
                }
            }
            else
            {
                item.Add(o);
            }
        }

        private void OnRemoveTempCommand(SatelliteCommandModel? o)
        {
            if (o == null) { return; }
            var t = _satCommandTemp.Find(t => t.Equals(o));
            if (t != null)
            {
                _satCommandTemp.Remove(t);
            }
        }

        private void OnGroupAdd(SatelliteCommandGroupModel? o)
        {
            if (o == null) { return; }
            _satCommandGroup.Add(o);
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

        public static List<SatelliteFunctionTypeModel> SearchAssociatedType(List<SatelliteFunctionTypeModel> types, string file, string name)
        {
            var list = new List<SatelliteFunctionTypeModel>();
            var ret = types.Where((o) => o.File == file && o.Name == name);
            if (ret.Count() > 1)
            {
                Logger.Error("Multiple Type Definitions");
                return list;
            }

            var tmp = ret.FirstOrDefault();
            if (tmp != null)
            {
                list.Add(tmp);
                foreach(var t in tmp.Parameters)
                {
                    list.AddRange(SearchAssociatedType(types, t.File, t.UserDefinedType));
                }
            }

            return list;
        }


        private static (SatelliteFunctionFileModel.DataType type, string str) TypeParseHelper(string str)
        {
            bool success = Enum.TryParse(str, ignoreCase: false, result: out DataType parse);
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

        public PaneFunctionPropertyViewModel? FindFunctionPropertyPane(SatelliteCommandModel command)
        {
            var pane = DocumentPane.FirstOrDefault(g => g is PaneFunctionPropertyViewModel p && p.Command.Equals(command));
            if (pane != null && pane is PaneFunctionPropertyViewModel p)
            {
                return p;
            }
            return null;
        }

        public void OpenFunctionPropertyPane(SatelliteMethodModel model)
        {
            var command = new SatelliteCommandModel(model);
            command.IsTemp = true;
            _satCommandTemp.Add(command);
            var pane = new PaneFunctionPropertyViewModel(this, command);
            DocumentPane.Add(pane);
        }

        public void OpenFunctionPropertyPane(SatelliteCommandModel model)
        {
            var item = FindFunctionPropertyPane(model);
            if (item == null)
            {
                var pane = new PaneFunctionPropertyViewModel(this, model);
                DocumentPane.Add(pane);
                ActiveDocument = pane;
            }
            else
            {
                ActiveDocument = item;
            }
        }
        public void OpenPropertyPreviewPane(SatelliteCommandModel model, bool changed)
        {
            var item = DocumentPane.FirstOrDefault(x => x is PanePropertyPreviewViewModel);
            PanePropertyPreview.CommandModel = model;
            if (item == null)
            {
                if(!changed)
                    DocumentPane.Add(PanePropertyPreview);
                else
                    ActiveDocument = item;
            }
            else
            {
                ActiveDocument = item;
            }
        }
        public bool DeleteCommandGroup(SatelliteCommandGroupModel model)
        {
            MessageBoxResult result = Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(TranslationSource.Instance["zAreYouSure"], TranslationSource.Instance["sDelete"],
                MessageBoxButton.OKCancel, MessageBoxImage.Question));

            if (result != MessageBoxResult.OK)
                return false;


            if (!SatelliteCommandGroup.Contains(model)) { return false; }
            _satCommandGroup.Remove(model);
            foreach(var c in model.Commands)
                FindFunctionPropertyPane(c)?.Close.Execute(null);

            return true;
        }

        public async void DeleteCommandGroupAll()
        {
            MessageBoxResult result = await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show( TranslationSource.Instance["zAreYouSure"], TranslationSource.Instance["sDeleteAll"],
                MessageBoxButton.OKCancel, MessageBoxImage.Question));

            if (result != MessageBoxResult.OK)
                return;

            for (int i = 0; i < SatelliteCommandGroup.Count;)
            {
                if (!DeleteCommandGroup(SatelliteCommandGroup[i]))
                    i++;
            }   
        }
        #endregion
    }
}
