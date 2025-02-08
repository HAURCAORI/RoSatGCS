using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PaneFunctionListViewModel : PaneViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private PageCommandViewModel _parent;
        private string _searchString = "";
        private SatelliteMethodModel? _selectedItem;
        

        public string SearchString
        {
            get { return _searchString; }
            set
            {
                SetProperty(ref _searchString, value);
                ApplyFilter.Execute(null);
            }
        }
        public SatelliteMethodModel? SelectedItem { get { return _selectedItem; } set { SetProperty(ref _selectedItem, value); } }

        public ICommand ApplyFilter { get; }
        public ICommand SearchClear { get; }
        public ICommand ListItemDoubleClick { get; }
        public ICommand LostListFocus { get; }
        public ICommand CheckAll { get; }
        public ICommand UncheckAll { get; }

        public PaneFunctionListViewModel(PageCommandViewModel viewModel)
        {
            _parent = viewModel;

            ApplyFilter = new RelayCommand(OnApplyFilter);
            SearchClear = new RelayCommand(OnSearchClear);
            ListItemDoubleClick = new RelayCommand(OnListItemDoubleClick);
            LostListFocus = new RelayCommand(OnLostListFocus);
            CheckAll = new RelayCommand(OnCheckAll);
            UncheckAll = new RelayCommand(OnUncheckAll);
        }

        private void OnApplyFilter()
        {
            _parent.SatelliteMethodView.Filter = new Predicate<object>(o => {
                return ((SatelliteMethodModel)o).Name.StartsWith(SearchString, StringComparison.OrdinalIgnoreCase)
                    && ((SatelliteMethodModel)o).Visibility == true;
            });
        }

        private void OnSearchClear()
        {
            SearchString = "";
        }

        private void OnListItemDoubleClick()
        {
            if (SelectedItem == null) { return; }

            var pane = new PaneFunctionPropertyViewModel    (_parent, SelectedItem);
            pane.Title = SelectedItem.Name;

            _parent.DocumentPane.Add(pane);
        }

        private void OnLostListFocus()
        {
            SelectedItem = null;
        }

        private void OnCheckAll()
        {
            foreach(var item in _parent.SatelliteMethod)
            {
                if (item.Visibility == true)
                    item.IsSelected = true;
                else
                    item.IsSelected = false;
            }
        }

        private void OnUncheckAll()
        {
            foreach (var item in _parent.SatelliteMethod)
            {
                item.IsSelected = false;
            }
        }
    }
}
