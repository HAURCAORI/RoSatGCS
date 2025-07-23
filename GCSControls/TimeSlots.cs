using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GCSControls
{
    /// <summary>
    /// XAML 파일에서 이 사용자 지정 컨트롤을 사용하려면 1a 또는 1b단계를 수행한 다음 2단계를 수행하십시오.
    ///
    /// 1a단계) 현재 프로젝트에 있는 XAML 파일에서 이 사용자 지정 컨트롤 사용.
    /// 이 XmlNamespace 특성을 사용할 마크업 파일의 루트 요소에 이 특성을 
    /// 추가합니다.
    ///
    ///     xmlns:MyNamespace="clr-namespace:GCSControls"
    ///
    ///
    /// 1b단계) 다른 프로젝트에 있는 XAML 파일에서 이 사용자 지정 컨트롤 사용.
    /// 이 XmlNamespace 특성을 사용할 마크업 파일의 루트 요소에 이 특성을 
    /// 추가합니다.
    ///
    ///     xmlns:MyNamespace="clr-namespace:GCSControls;assembly=GCSControls"
    ///
    /// 또한 XAML 파일이 있는 프로젝트의 프로젝트 참조를 이 프로젝트에 추가하고
    /// 다시 빌드하여 컴파일 오류를 방지해야 합니다.
    ///
    ///     솔루션 탐색기에서 대상 프로젝트를 마우스 오른쪽 단추로 클릭하고
    ///     [참조 추가]->[프로젝트]를 차례로 클릭한 다음 이 프로젝트를 찾아서 선택합니다.
    ///
    ///
    /// 2단계)
    /// 계속 진행하여 XAML 파일에서 컨트롤을 사용합니다.
    ///
    ///     <MyNamespace:TimeSlot/>
    ///
    /// </summary>
    /// 

    

    public class TimeSlots : Control, INotifyPropertyChanged
    {
        public const double kMajorTickWidth = 100.0; // Width of major ticks in pixels
        public const double kTimeSlotHeight = 20.0;

        private DrawingGroup _drawingGroup = new DrawingGroup();
        private bool _isTimeLineChanged = true;

        #region Dependency Properties
        public static readonly DependencyProperty TimeLineProperty = DependencyProperty.Register(
            nameof(TimeLine),
            typeof(TimeLine),
            typeof(TimeSlots),
            new FrameworkPropertyMetadata(
                new TimeLine(),
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnTimeLineChanged));

        public TimeLine TimeLine
        {
            get { return (TimeLine)GetValue(TimeLineProperty); }
            set { SetValue(TimeLineProperty, value); }
        }

        public static readonly DependencyProperty TimeSlotItemsProperty = DependencyProperty.Register(
            nameof(TimeSlotItems),
            typeof(ObservableCollection<TimeSlotItem>),
            typeof(TimeSlots),
            new FrameworkPropertyMetadata(new ObservableCollection<TimeSlotItem>(), FrameworkPropertyMetadataOptions.AffectsRender));

        public ObservableCollection<TimeSlotItem> TimeSlotItems
        {
            get { return (ObservableCollection<TimeSlotItem>)GetValue(TimeSlotItemsProperty); }
            set { SetValue(TimeSlotItemsProperty, value);
                TimeSlotItems.Add(new TimeSlotItem("Default", DateTime.Now, DateTime.Now.AddHours(1)));
            }
        }

        private static void OnTimeLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeSlots timeSlots)
            {
                if (e.OldValue is TimeLine oldTimeLine)
                {
                    oldTimeLine.PropertyChanged -= timeSlots.OnTimeLinePropertyChanged;
                }

                if (e.NewValue is TimeLine newTimeLine)
                {
                    newTimeLine.PropertyChanged += timeSlots.OnTimeLinePropertyChanged;
                }
            }
        }

        private void OnTimeLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimeLine.StartTime) || e.PropertyName == nameof(TimeLine.EndTime))
            {
                _isTimeLineChanged = true;
                InvalidateVisual();
                OnPropertyChanged(nameof(TimeLineStart));
                OnPropertyChanged(nameof(TimeLineEnd));
            }
        }

        public DateTime TimeLineStart => TimeLine?.StartTime ?? DateTime.MinValue;
        public DateTime TimeLineEnd => TimeLine?.EndTime ?? DateTime.MinValue;

        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register(
            nameof(VerticalOffset),
            typeof(double),
            typeof(TimeSlots),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }
        #endregion


        #region Drawing Properties

        private TimeSpan GetRoundedTimeInterval()
        {
            double width = ActualWidth;
            if (width <= 0 || TimeLine.EndTime <= TimeLine.StartTime)
                return TimeSpan.FromMinutes(5); // fallback

            double desiredNumTicks = width / kMajorTickWidth;
            TimeSpan rawInterval = (TimeLine.EndTime - TimeLine.StartTime) / desiredNumTicks;

            TimeSpan[] candidates = new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromHours(1),
                TimeSpan.FromHours(2),
                TimeSpan.FromHours(3),
                TimeSpan.FromHours(6),
                TimeSpan.FromHours(12),
                TimeSpan.FromDays(1)
            };

            foreach (var span in candidates)
            {
                if (rawInterval <= span)
                    return span;
            }

            return TimeSpan.FromDays(1); // default
        }

        private DateTime AlignTime(DateTime time, TimeSpan interval)
        {
            long ticks = time.Ticks / interval.Ticks * interval.Ticks;
            return new DateTime(ticks);
        }


        #endregion

        static TimeSlots()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeSlots), new FrameworkPropertyMetadata(typeof(TimeSlots)));
        }

        public void OnTimerTick()
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            RenderCurrentTimeLine(dc);
            RenderGrid();
            dc.DrawDrawing(_drawingGroup);

        }

        private void RenderCurrentTimeLine(DrawingContext dc)
        {
            if (TimeLine == null)
                return;

            DateTime now = DateTime.Now;
            if (now < TimeLine.StartTime || now > TimeLine.EndTime)
                return; // outside range

            double ratio = (now - TimeLine.StartTime).TotalMilliseconds / TimeLine.Interval.TotalMilliseconds;
            double x = ratio * ActualWidth;

            Brush red = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
            Pen redPen = new Pen(red, 1);
            dc.DrawLine(redPen, new Point(x, 0), new Point(x, ActualHeight));
        }

        private void RenderGrid()
        {
            if (!_isTimeLineChanged)
                return;
            DrawingContext dc = _drawingGroup.Open();
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
            DrawGrid(dc);
            dc.Pop();
            dc.Close();
        }

        private void DrawGrid(DrawingContext dc)
        {
            if (TimeLine == null || TimeLine.StartTime >= TimeLine.EndTime || ActualWidth <= 0 || ActualHeight <= 0)
                return;
            TimeSpan span = TimeLine.Interval;

            TimeSpan maj_interval = GetRoundedTimeInterval();
            DateTime t = AlignTime(TimeLine.StartTime, maj_interval);

            // Draw grid
            t = AlignTime(TimeLine.StartTime, maj_interval);
            for (; t <= TimeLine.EndTime; t += maj_interval)
            {
                double x = (t - TimeLine.StartTime).TotalMilliseconds / span.TotalMilliseconds * ActualWidth;
                if (x < 0 || x > ActualWidth)
                    continue; // Skip ticks outside the bounds

                Pen pen = new Pen(Brushes.WhiteSmoke, 1);
                dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
