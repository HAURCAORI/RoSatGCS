﻿using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RoSatGCS.ViewModels
{
    public class PaneFunctionListViewModel : PaneViewModel
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
        private ObservableCollection<SatelliteMethodModel> _selectedItems = [];
        private SatelliteCommandGroupModel? _selectedCommandGroup;


        public string SearchString
        {
            get { return _searchString; }
            set
            {
                SetProperty(ref _searchString, value);
                ApplyFilter.Execute(null);
            }
        }
        public ObservableCollection<SatelliteMethodModel> SelectedItems { get { return _selectedItems; } set { SetProperty(ref _selectedItems, value); } }
        public bool IsSingleSelection { get => SelectedItems?.Count == 1; }
        public SatelliteCommandGroupModel? SelectedCommandGroup { get { return _selectedCommandGroup; } set { SetProperty(ref _selectedCommandGroup, value); } }

        public ICommand ApplyFilter { get; }
        public ICommand SearchClear { get; }
        public ICommand ListItemDoubleClick { get; }
        public ICommand LostListFocus { get; }
        public ICommand CheckAll { get; }
        public ICommand UncheckAll { get; }
        public ICommand Select { get; }
        public ICommand Deselect { get; }
        public ICommand AddToCommand { get; }
        public ICommand AddToCommandSelected { get; }
        public ICommand UpdateSelectedItems { get; }

        public PaneFunctionListViewModel(PageCommandViewModel viewModel)
        {
            _parent = new WeakReference<PageCommandViewModel>(viewModel);

            ApplyFilter = new RelayCommand(OnApplyFilter);
            SearchClear = new RelayCommand(OnSearchClear);
            ListItemDoubleClick = new RelayCommand(OnListItemDoubleClick);
            LostListFocus = new RelayCommand<object>(OnLostListFocus);
            CheckAll = new RelayCommand(OnCheckAll);
            UncheckAll = new RelayCommand(OnUncheckAll);
            Select = new RelayCommand(OnSelect);
            Deselect = new RelayCommand(OnDeselect);
            AddToCommand = new RelayCommand(OnAddToCommand);
            AddToCommandSelected = new RelayCommand(OnAddToCommandSelected);
            UpdateSelectedItems = new RelayCommand<object>(OnUpdateSelectedItems);
        }

        private void OnApplyFilter()
        {
            if (Parent != null)
                Parent.SatelliteMethodView.Filter = new Predicate<object>(o =>
                {
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
            if (SelectedItems == null) { return; }
            if (Parent == null) { return; }
            if (!IsSingleSelection) { return; }
            Parent.OpenFunctionPropertyPane(SelectedItems.First());
        }

        private void OnLostListFocus(object? o)
        {
            if (o is System.Windows.Controls.ListView l)
            {
                if (!l.IsMouseOver)
                    l.SelectedItems.Clear();
            }
        }

        private void OnCheckAll()
        {
            if (Parent == null) { return; }
            foreach (var item in Parent.SatelliteMethod)
            {
                if (item.Visibility == true)
                    item.IsSelected = true;
                else
                    item.IsSelected = false;
            }
        }

        private void OnUncheckAll()
        {
            if (Parent == null) { return; }
            foreach (var item in Parent.SatelliteMethod)
            {
                item.IsSelected = false;
            }
        }

        private void OnSelect()
        {
            foreach (var item in SelectedItems)
            {
                item.IsSelected = true;
            }
        }
        private void OnDeselect()
        {
            foreach (var item in SelectedItems)
            {
                item.IsSelected = false;
            }
        }

        private void OnAddToCommand()
        {
            if (Parent == null) { return; }

            string? groupName = null;
            if(SelectedCommandGroup != null)
            {
                groupName = SelectedCommandGroup.Name;
            }

            Parent.SatelliteMethod.Where(o => o.IsSelected).ToList().ForEach(o => Parent.AddCommand.Execute(new SatelliteCommandModel(o) { GroupName = groupName}));

        }
        private void OnAddToCommandSelected()
        {
            if (Parent == null) { return; }
            string? groupName = null;
            if (SelectedCommandGroup != null)
            {
                groupName = SelectedCommandGroup.Name;
            }
            foreach(var o in SelectedItems)
            {
                Parent.AddCommand.Execute(new SatelliteCommandModel(o) { GroupName = groupName });
            }
        }
        private void OnUpdateSelectedItems(object? o)
        {
            if (o is System.Collections.IList s)
            {
                SelectedItems.Clear();
                foreach (var c in s.Cast<SatelliteMethodModel>())
                {
                    SelectedItems.Add(c);
                }
                OnPropertyChanged(nameof(IsSingleSelection));
            }
        }

    }
}
