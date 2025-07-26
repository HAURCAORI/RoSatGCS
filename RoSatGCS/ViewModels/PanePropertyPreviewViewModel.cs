using AvalonDock.Controls;
using CommunityToolkit.Mvvm.Input;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace RoSatGCS.ViewModels
{
    public class PanePropertyPreviewViewModel : PaneViewModel
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private WeakReference<SatelliteCommandModel>? _commandModel;

        public SatelliteCommandModel? CommandModel
        {
            get
            {
                if (_commandModel != null && _commandModel.TryGetTarget(out var target))
                    return target;
                return null;
            }
            set
            {
                if (value != null)
                    _commandModel = new WeakReference<SatelliteCommandModel>(value);
                OnPropertyChanged(nameof(CommandModel));
            }
        }
        
        private bool _initialized = false;
        private int _column = 2;
        public int GridColumn { get { return _column; } set { SetProperty(ref _column, value); } }

        public ICommand Close { get; set; }
        public ICommand SizeChanged { get; set; }

        public PanePropertyPreviewViewModel()
        {
            Close = new RelayCommand(OnClose);
            SizeChanged = new RelayCommand<object>(OnSizeChanged);
        }

        private void OnClose()
        {
            var Parent = MainDataContext.Instance.GetPageCommandViewModel;
            Parent?.CloseDocument(this);
        }

        private void OnSizeChanged(object? sender)
        {
            if (sender is FrameworkElement args)
            {
                var grid = args.FindVisualChildren<UniformGrid>().FirstOrDefault();

                if (grid == null) return;

                GridColumn = Math.Max(1, (int)(args.ActualWidth / 180));

                args.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var c in grid.Children)
                    {
                        if (c is FrameworkElement e)
                        {
                            var g = e.FindVisualChildren<Grid>().FirstOrDefault();
                            if (g != null)
                            {

                                g.Width = e.ActualWidth - SystemParameters.VerticalScrollBarWidth;
                            }
                        }
                    }
                }, DispatcherPriority.Loaded);
            }
        }
    }
}
