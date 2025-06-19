using CommunityToolkit.Mvvm.Input;
using GMap.NET.Internals;
using Newtonsoft.Json.Linq;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PaneTypeDictionaryViewModel : PaneViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private WeakReference<PageCommandViewModel> _parent;
        public PageCommandViewModel? Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out var target))
                    return target;
                return null;
            }
        }


        private string _searchString = "";
        private SatelliteFunctionTypeModel? _selectedItem;

        public string SearchString
        {
            get { return _searchString; }
            set
            {
                SetProperty(ref _searchString, value);
                ApplyFilter.Execute(null);
            }
        }

        public SatelliteFunctionTypeModel? SelectedItem { get { return _selectedItem; } set { SetProperty(ref _selectedItem, value); } }

        public ICommand ApplyFilter { get; }
        public ICommand SearchClear { get; }
        public ICommand ListItemDoubleClick { get; }
        public ICommand LostListFocus { get; }
        

        public PaneTypeDictionaryViewModel(PageCommandViewModel viewModel)
        {
            _parent = new WeakReference<PageCommandViewModel>(viewModel);

            ApplyFilter = new RelayCommand(OnApplyFilter);
            SearchClear = new RelayCommand(OnSearchClear);
            ListItemDoubleClick = new RelayCommand(OnListItemDoubleClick);
            LostListFocus = new RelayCommand(OnLostListFocus);
        }
        private void OnApplyFilter()
        {
            if (Parent != null)
                Parent.SatelliteFunctionTypesView.Filter = new Predicate<object>(o =>
                {
                    return ((SatelliteFunctionTypeModel)o).Name.StartsWith(SearchString, StringComparison.OrdinalIgnoreCase)
                        && ((SatelliteFunctionTypeModel)o).Visibility == true;
                });
        }

        private void OnSearchClear()
        {
            SearchString = "";
        }

        private void OnListItemDoubleClick()
        {
            if (SelectedItem == null) { return; }
            if (Parent == null) { return; }

            var pane = new PaneTypeSummaryViewModel(Parent);
            pane.Title = SelectedItem.Name;
            pane.id = (SelectedItem.Name + SelectedItem.File).GetHashCode();
            pane.SatFuncType = SelectedItem;

            var item = Parent.DocumentPane.FirstOrDefault(x => x.id == pane.id);

            if (item == null)
            {
                Parent.DocumentPane.Add(pane);
            }
            else
            {
                Parent.ActiveDocument = item;
            }
        }

        private void OnLostListFocus()
        {
            SelectedItem = null;
        }

    }
}
