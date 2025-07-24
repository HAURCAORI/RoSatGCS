using CommunityToolkit.Mvvm.Input;
using NLog;
using RoSatGCS.Controls;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using RoSatGCS.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PaneCommandFileViewModel : PaneViewModel
    {
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

        private SatelliteFunctionFileModel? _selectedItem;

        public bool Visibility { get; set; }

        public SatelliteFunctionFileModel? SelectedItem { get { return _selectedItem; } set { SetProperty(ref _selectedItem, value); } }

        public ICommand AddFile { get; }
        public ICommand AddFileFromDrag { get; }
        public ICommand RemoveFile { get; }
        public ICommand LostListFocus { get; }
        public ICommand RefreshFile { get; }



        public PaneCommandFileViewModel(PageCommandViewModel viewModel) {
            _parent = new WeakReference<PageCommandViewModel>(viewModel);
            AddFile = new RelayCommand(AddFileFunc);
            AddFileFromDrag = new RelayCommand<object>(AddFileFromDragFunc);
            RemoveFile = new RelayCommand<SatelliteFunctionFileModel>(RemoveFileFunc);
            LostListFocus = new RelayCommand(OnLostListFocus);
            RefreshFile = new RelayCommand<SatelliteFunctionFileModel>(OnRefreshFile);
        }

        private void AddFileFunc()
        {
            var dialogue = new WindowFileSearch(["json"]);
            if (dialogue.ShowDialog() == true)
            {
                AddFileHelper([dialogue.Path]);
            }
        }

        private void AddFileFromDragFunc(object? o)
        {
            if (o is string[] list)
            {
                AddFileHelper(list);
            }
        }

        private void AddFileHelper(string[] list)
        {
            if (Parent == null) { return; }
            List<string> error = [];
            
            foreach (string s in list)
            {
                var item = MainDataContext.Instance.SatFuncFile.Where(x => x.FilePath == s).FirstOrDefault();
                if (item != null)
                {
                    var ret = System.Windows.MessageBox.Show(TranslationSource.Instance["zSameFileExists"] + ":\r\n  " + s + "\r\n" + TranslationSource.Instance["zReplaceIt"], TranslationSource.Instance["sWarning"], MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (ret == MessageBoxResult.Yes)
                    {
                        Parent.RemoveSatFuncFile.Execute(item);
                    }
                    else
                    {
                        continue;
                    }
                }

                var ffm = new SatelliteFunctionFileModel();
                try
                {
                    ffm.Initialize(s);
                    Parent.AddSatFuncFile.Execute(ffm);
                    
                }
                catch (Exception ex)
                {
                    error.Add(s);
                    Logger.Error(ex);
                }
            }

            if (error.Count > 0)
            {
                MessageBox.Show(TranslationSource.Instance["zFailToLoad"] + ":\r\n" + string.Join("\r\n", error), TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveFileFunc(object? o)
        {
            if (Parent == null) { return; }
            if (o is SatelliteFunctionFileModel item)
            {
                var ret = MessageBox.Show(TranslationSource.Instance["zAreYouSure"], TranslationSource.Instance["sRemove"], MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (ret == MessageBoxResult.Yes)
                {
                    Parent.RemoveSatFuncFile.Execute(item);
                }
            }
        }
        private void OnLostListFocus()
        {
            SelectedItem = null;
        }

        private void OnRefreshFile(object? o)
        {
            if (Parent == null) { return; }
            if (o is SatelliteFunctionFileModel item)
            {
                var path = item.FilePath;
                Parent.RemoveSatFuncFile.Execute(item);
                AddFileHelper([path]);
            }
        }
    }
}
