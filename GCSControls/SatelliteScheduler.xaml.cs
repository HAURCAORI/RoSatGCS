using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GCSControls
{
    /// <summary>
    /// Interaction logic for SatelliteScheduler.xaml
    /// </summary>
    public partial class SatelliteScheduler : UserControl
    {

        #region Dependency Properties
        public static readonly DependencyProperty TimeLineProperty = DependencyProperty.Register(
            nameof(TimeLine),
            typeof(TimeLine),
            typeof(SatelliteScheduler),
            new FrameworkPropertyMetadata(new TimeLine(), FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public TimeLine TimeLine
        {
            get { return (TimeLine)GetValue(TimeLineProperty); }
            set { SetValue(TimeLineProperty, value); }
        }

        private static readonly DependencyProperty EnableAutoPanningProperty = DependencyProperty.Register(
            nameof(EnableAutoPanning),
            typeof(bool),
            typeof(SatelliteScheduler),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool EnableAutoPanning
        {
            get { return (bool)GetValue(EnableAutoPanningProperty); }
            set { SetValue(EnableAutoPanningProperty, value); }
        }

        public static readonly DependencyProperty TimeSlotItemsProperty = DependencyProperty.Register(
            nameof(TimeSlotItems),
            typeof(ObservableCollection<TimeSlotItem>),
            typeof(SatelliteScheduler),
            new FrameworkPropertyMetadata(null));

        public ObservableCollection<TimeSlotItem> TimeSlotItems
        {
            get { return (ObservableCollection<TimeSlotItem>)GetValue(TimeSlotItemsProperty); }
            set { SetValue(TimeSlotItemsProperty, value); }
        }


        #endregion

        private async Task Update()
        {
            while (true)
            {
                await Task.Delay(32);
                xTimeBar.OnTimerTick(EnableAutoPanning);
                xTimeSlots.OnTimerTick();
            }
        }

        public SatelliteScheduler()
        {
            InitializeComponent();

            TimeLine = new TimeLine();
            this.DataContext = this;

            _ = Update();
            Loaded += (s, e) => SyncScrollViewer();
        }

        private void SyncScrollViewer()
        {
            var scrollViewer = FindScrollViewer(xListBox);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += (s, e) =>
                {
                    xTimeSlots.VerticalOffset = scrollViewer.VerticalOffset * TimeSlots.kTimeSlotHeight;
                };
            }
        }

        private ScrollViewer? FindScrollViewer(DependencyObject? element)
        {
            if (element == null) return null;
            if (element is ScrollViewer viewer)
            {
                return viewer;
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var result = FindScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
