using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessagePack;
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
using static RoSatGCS.Models.SatelliteFunctionFileModel;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class SatelliteCommandGroupModel : ObservableObject, IDisposable
    {
        [IgnoreMember]
        private readonly WeakReference<PageCommandViewModel>? _parent;
        [IgnoreMember]
        public PageCommandViewModel? Parent
        {
            get
            {
                if (_parent != null && _parent.TryGetTarget(out var target))
                    return target;
                return null;
            }
        }

        #region Fields
        [IgnoreMember]
        private bool _disposed = false;
        [Key("isre")]
        private bool _isRename = false;
        [Key("name")]
        private string _name = "";
        [Key("rena")]
        private string? _rename = null;
        [Key("elap")]
        private string _elapsedTime = "";
        [Key("time")]
        private DateTime _lastUpdatedTime = DateTime.UtcNow;
        [Key("comm")]
        private ObservableCollection<SatelliteCommandModel> _commands = [];
        [IgnoreMember]
        private ObservableCollection<SatelliteCommandModel> _selectedItems = [];
        [IgnoreMember]
        private RelayCommand<object>? _lostListFocusCommand;
        [IgnoreMember]
        private RelayCommand? _listItemDoubleClick;
        [IgnoreMember]
        private RelayCommand? _deleteCommandGroup;
        [IgnoreMember]
        private RelayCommand? _renameCommandGroup;
        [IgnoreMember]
        private RelayCommand? _applyRename;
        [IgnoreMember]
        private RelayCommand? _renameKeyDown;
        [IgnoreMember]
        private RelayCommand? _deleteItem;
        [IgnoreMember]
        private RelayCommand<object>? _updatedSelectedItems;
        [IgnoreMember]
        private RelayCommand? _openProperty;
        [IgnoreMember]
        private RelayCommand<bool>? _openPreview;
        [IgnoreMember]
        private RelayCommand? _executeOnce;
        [IgnoreMember]
        private RelayCommand? _executeAll;
        #endregion

        #region Properties
        [IgnoreMember]
        public bool IsRename
        {
            get =>_isRename;
            set
            {
                if(SetProperty(ref _isRename, value))
                    OnPropertyChanged(nameof(IsNotRename));
            }
        }
        [IgnoreMember]
        public bool IsNotRename { get => !IsRename; }
        [IgnoreMember]
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        [IgnoreMember]
        public string? Rename
        {
            get { return _rename ?? Name; }
            set => SetProperty(ref _rename, value);
        }
        [IgnoreMember]
        public string ElapsedTime { get => (_elapsedTime == string.Empty) ? "-" : _elapsedTime; internal set => SetProperty(ref _elapsedTime, value); }
        [IgnoreMember]
        public DateTime LastUpdatedTime
        {
            get => _lastUpdatedTime;
            set
            {
                _lastUpdatedTime = value;
                UpdateElapsedTime();
            }
        }
        [IgnoreMember]
        public ObservableCollection<SatelliteCommandModel> Commands
        {
            get => _commands;
        }
        [IgnoreMember]
        public ObservableCollection<SatelliteCommandModel> SelectedItems { get { return _selectedItems; } set { SetProperty(ref _selectedItems, value); } }
        [IgnoreMember]
        public bool IsSingleSelection { get => SelectedItems?.Count == 1; }
        #endregion

        #region Commands
        [IgnoreMember]
        public ICommand LostListFocus { get => _lostListFocusCommand ??= new RelayCommand<object>(OnLostListFocus); }
        [IgnoreMember]
        public ICommand ListItemDoubleClick { get => _listItemDoubleClick ??= new RelayCommand(OnListItemDoubleClick); }
        [IgnoreMember]
        public ICommand DeleteCommandGroup { get => _deleteCommandGroup ??= new RelayCommand(OnDeleteCommandGroup); }
        [IgnoreMember]
        public ICommand RenameCommandGroup { get => _renameCommandGroup ??= new RelayCommand(OnRenameCommandGroup); }
        [IgnoreMember]
        public ICommand ApplyRename { get => _applyRename ??= new RelayCommand(OnApplyRename); }
        [IgnoreMember]
        public ICommand RenameKeyDown { get => _renameKeyDown ??= new RelayCommand(OnRenameKeyDown); }
        [IgnoreMember]
        public ICommand DeleteItem { get => _deleteItem ??= new RelayCommand(OnDeleteItem); }
        [IgnoreMember]
        public ICommand UpdateSelectedItems { get => _updatedSelectedItems ??= new RelayCommand<object>(OnUpdateSelectedItems); }
        [IgnoreMember]
        public ICommand OpenProperty { get => _openProperty ??= new RelayCommand(OnOpenProperty); }
        [IgnoreMember]
        public ICommand OpenPreview { get => _openPreview ??= new RelayCommand<bool>(OnOpenPreview); }
        [IgnoreMember]
        public ICommand ExecuteOnce { get => _executeOnce ??= new RelayCommand(OnExecuteOnce); }
        [IgnoreMember]
        public ICommand ExecuteAll { get => _executeAll??= new RelayCommand(OnExecuteAll); }
        #endregion

        private SatelliteCommandGroupModel() { }
        public SatelliteCommandGroupModel(PageCommandViewModel parent, SatelliteCommandGroupModel model)
        {
            _parent = new WeakReference<PageCommandViewModel>(parent);
            _isRename = model.IsRename;
            _name = model.Name;
            _rename = model.Rename;
            _elapsedTime = model.ElapsedTime;
            _lastUpdatedTime = model.LastUpdatedTime;
            foreach (var item in model.Commands)
            {
                _commands.Add(item);
            }
            TimeUpdateManager.Instance.Subscribe(UpdateElapsedTime);
        }
        public SatelliteCommandGroupModel(PageCommandViewModel parent, string name)
        {
            _parent = new WeakReference<PageCommandViewModel>(parent);
            _name = name;
            TimeUpdateManager.Instance.Subscribe(UpdateElapsedTime);
        }

        ~SatelliteCommandGroupModel() { Dispose(false); }

        private void OnLostListFocus(object? o)
        {
            if(o is System.Windows.Controls.ListBox l)
            {
                if(!l.IsMouseOver)
                    l.SelectedItems.Clear();
            }
        }

        private void OnListItemDoubleClick()
        {
            if (SelectedItems == null) { return; }
            if (IsSingleSelection)
            {
                Parent?.OpenFunctionPropertyPane(SelectedItems.First());
            }
        }

        private void OnDeleteCommandGroup()
        {
            if (Parent == null) { return; }
            this.Dispose();
            Parent.DeleteCommandGroup(this);
        }

        private void OnRenameCommandGroup()
        {
            IsRename = !IsRename;
        }

        private void OnApplyRename()
        {
            IsRename = !IsRename;
            if (Rename == null) { return; }
            if (Parent == null) { Rename = null; return; }
            if (Name == Rename) { Rename = null; return; }

            if (Parent.SatelliteCommandGroup.FirstOrDefault(o => o.Name == Rename) != null)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(TranslationSource.Instance["zSameCommandExists"] + ": " + Rename,
                    TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning));
            }
            else
            {
                Name = Rename;
                foreach (var item in Commands)
                {
                    item.GroupName = Name;
                    Parent.FindFunctionPropertyPane(item)?.UpdateTitle();
                }
            }

            Rename = null;
        } 

        private void OnRenameKeyDown()
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                OnApplyRename();
            }
            else if (Keyboard.IsKeyDown(Key.Escape))
            {
                IsRename = !IsRename;
                Rename = null;
            }
        }

        private async void OnDeleteItem()
        {
            if (SelectedItems == null) { return; }
            if (!SelectedItems.Any()) { return; }
            MessageBoxResult result = await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(TranslationSource.Instance["zAreYouSure"], TranslationSource.Instance["sDelete"],
                MessageBoxButton.OKCancel, MessageBoxImage.Question));

            if (result != MessageBoxResult.OK)
                return;

            for (int i = 0; i < SelectedItems.Count;)
            {
                if (!Commands.Contains(SelectedItems[i])) { i++; continue; }
                Commands.Remove(SelectedItems[i]);
            }
            
            var j = 1;
            foreach (var c in Commands)
            {
                c.GroupIndex = j++;
            }
        }

        public void DeleteCommandAndDispose(SatelliteCommandModel model)
        {
            if (!Commands.Contains(model)) { return; }
            model.Dispose();
            Commands.Remove(model);
            var j = 1;
            foreach (var c in Commands)
            {
                c.GroupIndex = j++;
            }
        }


        private void OnUpdateSelectedItems(object? o)
        {
            if (o is System.Collections.IList s)
            {
                SelectedItems.Clear();
                foreach (var c in s.Cast<SatelliteCommandModel>())
                {
                    SelectedItems.Add(c);
                }
                OnPropertyChanged(nameof(IsSingleSelection));
                OpenPreview.Execute(true);
            }
        }

        private void OnOpenProperty()
        {
            if (SelectedItems == null) { return; }
            if (IsSingleSelection)
            {
                Parent?.OpenFunctionPropertyPane(SelectedItems.First());
            }
        }

        private void OnOpenPreview(bool value)
        {
            if (SelectedItems == null) { return; }
            if (IsSingleSelection)
            {
                Parent?.OpenPropertyPreviewPane(SelectedItems.First(), value);
            }
        }

        private void OnExecuteOnce()
        {
            if (SelectedItems == null) { return; }
            if (IsSingleSelection)
            {
                if(SelectedItems.First().IsValid)
                    SelectedItems.First().Execute.Execute(null);
                else
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show(TranslationSource.Instance["zParametersInvalid"],
                    TranslationSource.Instance["sInvalid"], MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }

        private async void OnExecuteAll()
        {
            if(Commands.Any(o => !o.IsValid))
            {
               MessageBoxResult result = await Application.Current.Dispatcher.InvokeAsync(() =>
               MessageBox.Show(TranslationSource.Instance["zParametersInvalid"] + "\r\n" + TranslationSource.Instance["zExecuteAnyway"], TranslationSource.Instance["sInvalid"],
               MessageBoxButton.OKCancel, MessageBoxImage.Question));

                if (result != MessageBoxResult.OK)
                    return;
            }

            foreach (var item in Commands)
            {
                if (item.IsValid)
                    item.Execute.Execute(null);
            }
            _lastUpdatedTime = DateTime.UtcNow;
            UpdateElapsedTime();
        }

        public SatelliteCommandModel? Add(SatelliteCommandModel command, bool withoutClone = false)
        {
            var model = withoutClone ? command : (SatelliteCommandModel)command.Clone();
            model.GroupName = Name;
            if (Commands.Contains(model))
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show(TranslationSource.Instance["zSameCommandExists"] + ": " + model.Name,
                    TranslationSource.Instance["sWarning"], MessageBoxButton.OK, MessageBoxImage.Warning));
                return null;
            }

            Commands.Add(model);

            var i = 1;
            foreach (var c in Commands)
            {
                c.GroupIndex = i++;
            }
            return model;
        }

        public void Reorder(SatelliteCommandModel model, int index)
        {
            if (!Commands.Contains(model)) { return; }
            if (index < 0 || index >= Commands.Count) { return; }
            Commands.Remove(model);
            Commands.Insert(index, model);
            var i = 1;
            foreach (var c in Commands)
            {
                c.GroupIndex = i++;
            }
        }
        private void UpdateElapsedTime()
        {
            var elapsed = DateTime.UtcNow - _lastUpdatedTime;
            ElapsedTime = TimeUpdateManager.FormatElapsedTime(elapsed);
        }

        public void Init()
        {
            foreach (var item in Commands)
            {
                item.Init();
            }
        }

        #region Dispose
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
                _commands.Clear();
                TimeUpdateManager.Instance.Unsubscribe(UpdateElapsedTime);
            }

            _disposed = true;
        }
        #endregion
    }
}