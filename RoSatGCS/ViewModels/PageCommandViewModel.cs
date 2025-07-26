using AvalonDock;
using AvalonDock.Layout;
using CommunityToolkit.Mvvm.Input;
using MessagePack;
using NLog.Layouts;
using RoSatGCS.Controls;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using static NetMQ.NetMQSelector;
using static RoSatGCS.Models.SatelliteFunctionFileModel;

namespace RoSatGCS.ViewModels
{
       
    public class PageCommandViewModel : ViewModelPageBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //private static readonly string PathCacheFuncFile = "Cache/func_file_list";
        //private static readonly string PathCacheCommandFile = "Cache/command_list";

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
        public ListCollectionView SatelliteFunctionFileView { get => MainDataContext.Instance.SatelliteFunctionFileView; }

        // Sat Func Type
        public ListCollectionView SatelliteFunctionTypesView { get => MainDataContext.Instance.SatelliteFunctionTypesView; }

        // Sat Method
        public ListCollectionView SatelliteMethodView { get => MainDataContext.Instance.SatelliteMethodView; }

        // Sat Command
        public ListCollectionView SatelliteCommandGroupView { get => MainDataContext.Instance.SatelliteCommandGroupView; }

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

            _paneCommandFile = new WeakReference<PaneCommandFileViewModel>(new PaneCommandFileViewModel());
            if (PaneCommandFile != null)
                _anchorable.Add(PaneCommandFile);

            _paneTypeDictionary = new WeakReference<PaneTypeDictionaryViewModel>(new PaneTypeDictionaryViewModel());
            if (PaneTypeDictionary != null)
                _anchorable.Add(PaneTypeDictionary);

            _paneFunctionList = new WeakReference<PaneFunctionListViewModel>(new PaneFunctionListViewModel());
            if (PaneFunctionList != null)
                _document.Add(PaneFunctionList);

            _paneCommandSet = new WeakReference<PaneCommandSetViewModel>(new PaneCommandSetViewModel());
            if (PaneCommandSet != null)
                _document.Add(PaneCommandSet);

            _panePropertyPreview = new PanePropertyPreviewViewModel();
        }
        #endregion


        #region Command Implementation
        private void OnLoaded()
        {
            if(initialized) { return; }
     
            
            RefreshAll.Execute(null);
            initialized = true;
        }

        private void OnClosing()
        {
            if (!initialized) { return; }
        }

        private void OnAddSatFuncFile(SatelliteFunctionFileModel? model)
        {
            if(model != null)
            {
                MainDataContext.Instance.AddSatelliteFunctionFile(model);

                OnRefresh(model);
            }
        }

        private void OnRemoveSatFuncFile(SatelliteFunctionFileModel? model)
        {
            if (model != null)
            {
                // Remove all related type
                var e_type = MainDataContext.Instance.SatelliteFunctionTypes.Where(t => t.File == model.Name).ToList();
                foreach (var t in e_type)
                    MainDataContext.Instance.SatelliteFunctionTypes.Remove(t);

                // Remove all related method
                var e_method = MainDataContext.Instance.SatelliteMethod.Where(t => t.File == model.Name).ToList();
                foreach (var t in e_method)
                    MainDataContext.Instance.SatelliteMethod.Remove(t);

                // Remove function file
                MainDataContext.Instance.RemoveSatelliteFunctionFile(model);
            }
        }

        private void OnRefresh(SatelliteFunctionFileModel? model)
        {
            if (model == null) { return; }

            bool error = false;
            model.Valid = false;
            
            var file = MainDataContext.Instance.SatFuncFile.FirstOrDefault(t => t.Name == model.Name);
            if (file == null) { return; }
            if (file.Structure == null) { return; }

            // Remove all related type
            var e_type = MainDataContext.Instance.SatelliteFunctionTypes.Where(t => t.File == model.Name).ToList();
            foreach (var t in e_type)
                MainDataContext.Instance.SatelliteFunctionTypes.Remove(t);

            // Remove all related method
            var e_method = MainDataContext.Instance.SatelliteMethod.Where(t => t.File == model.Name).ToList();
            foreach (var t in e_method)
                MainDataContext.Instance.SatelliteMethod.Remove(t);

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
                    MainDataContext.Instance.SatelliteFunctionTypes.Add(i);
                foreach (var i in tempSatMethod)
                    MainDataContext.Instance.SatelliteMethod.Add(i);
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
            MainDataContext.Instance.SatelliteFunctionTypes.Clear();
            foreach (var item in MainDataContext.Instance.SatFuncFile)
            {
                OnRefresh(item);
            }
        }
        
        private void OnTypeHyperLinkClick(object? sender)
        {
            if (sender is ParameterModel param)
            {
                if (param.Name == "" || !param.IsUserDefined) { return; }

                var search = SearchType([.. MainDataContext.Instance.SatelliteFunctionTypes], param.File, param.UserDefinedType);
                if (search == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show(TranslationSource.Instance["zTypeNotFound"] +": "+ param.UserDefinedType, TranslationSource.Instance["sError"], MessageBoxButton.OK, MessageBoxImage.Error));
                    Logger.Error("Type not found: " + param.UserDefinedType);
                    return;
                }

                var pane = new PaneTypeSummaryViewModel()
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
                var e_type = MainDataContext.Instance.SatelliteFunctionTypes.Where(t => t.File == model.Name).ToList();
                foreach (var t in e_type)
                    t.Visibility = model.Visibility;
                PaneTypeDictionary?.ApplyFilter.Execute(null);

                // Remove all related method
                var e_method = MainDataContext.Instance.SatelliteMethod.Where(t => t.File == model.Name).ToList();
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

            var item = MainDataContext.Instance.SatelliteCommandGroup.FirstOrDefault(g => g.Name == o.GroupName);
            if(item == null)
            {
                item = new SatelliteCommandGroupModel(o.GroupName);
                MainDataContext.Instance.AddSatelliteCommandGroup(item);
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
            MainDataContext.Instance.AddSatelliteCommandGroup(o);
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
            var pane = new PaneFunctionPropertyViewModel(command);
            DocumentPane.Add(pane);
        }

        public void OpenFunctionPropertyPane(SatelliteCommandModel model)
        {
            var item = FindFunctionPropertyPane(model);
            if (item == null)
            {
                var pane = new PaneFunctionPropertyViewModel(model);
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


            if (!MainDataContext.Instance.SatelliteCommandGroup.Contains(model)) { return false; }
            MainDataContext.Instance.RemoveSatelliteCommandGroup(model);
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

            for (int i = 0; i < MainDataContext.Instance.SatelliteCommandGroup.Count;)
            {
                if (!DeleteCommandGroup(MainDataContext.Instance.SatelliteCommandGroup[i]))
                    i++;
            }   
        }
        #endregion
    }
}
