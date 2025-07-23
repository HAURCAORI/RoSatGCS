using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
    ///     <MyNamespace:TimeBar/>
    ///
    /// </summary>
    public class TimeBar : Control, INotifyPropertyChanged
    {
        #region Private Fields
        private const double _kmajorTickWidth = 100.0; // Width of major ticks in pixels
        private const int _kminorTickCount = 5; // Number of minor ticks between major ticks

        private DrawingGroup _drawingGroup = new DrawingGroup();
        private bool _isTimeLineChanged = true;
        #endregion


        #region Dependency Properties
        public static readonly DependencyProperty TimeLineProperty = DependencyProperty.Register(
            nameof(TimeLine),
            typeof(TimeLine),
            typeof(TimeBar),
            new FrameworkPropertyMetadata(
                new TimeLine(),
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnTimeLineChanged));

        public TimeLine TimeLine
        {
            get { return (TimeLine)GetValue(TimeLineProperty); }
            set { SetValue(TimeLineProperty, value); }
        }
        private static void OnTimeLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimeBar timeBar)
            {
                if (e.OldValue is TimeLine oldTimeLine)
                {
                    oldTimeLine.PropertyChanged -= timeBar.OnTimeLinePropertyChanged;
                }

                if (e.NewValue is TimeLine newTimeLine)
                {
                    newTimeLine.PropertyChanged += timeBar.OnTimeLinePropertyChanged;
                }
            }
        }

        private void OnTimeLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimeLine.StartTime) || e.PropertyName == nameof(TimeLine.EndTime))
            {
                _isTimeLineChanged = true;
                InvalidateVisual();
            }
        }

        #endregion

        #region Properties


        #endregion

        #region Drawing Properties

        private TimeSpan GetRoundedTimeInterval()
        {
            double width = ActualWidth;
            if (width <= 0 || TimeLine.EndTime <= TimeLine.StartTime)
                return TimeSpan.FromMinutes(5); // fallback

            double desiredNumTicks = width / _kmajorTickWidth;
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

        static TimeBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeBar), new FrameworkPropertyMetadata(typeof(TimeBar)));
        }

        public void OnTimerTick(bool autoPanning)
        {
            if (autoPanning && DateTime.Now >= TimeLine.EndTime)
            {
                TimeSpan span = TimeLine.Interval;
                TimeLine.Shift(span);
            }    

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            RenderCurrentTimeLine(dc);
            RenderTick();
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

        private void RenderTick()
        {
            if (!_isTimeLineChanged)
                return;
            DrawingContext dc = _drawingGroup.Open();
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
            DrawTicks(dc);
            dc.Pop();
            dc.Close();
        }

        private void DrawTicks(DrawingContext dc)
        {
            if (TimeLine == null || TimeLine.StartTime >= TimeLine.EndTime || ActualWidth <= 0 || ActualHeight <= 0)
                return;
            TimeSpan span = TimeLine.Interval;

            TimeSpan maj_interval = GetRoundedTimeInterval();
            TimeSpan min_interval = TimeSpan.FromTicks(maj_interval.Ticks / _kminorTickCount);
            DateTime t = AlignTime(TimeLine.StartTime, maj_interval);

            // Draw minor ticks
            for (; t < TimeLine.EndTime; t += min_interval)
            {
                double x = (t - TimeLine.StartTime).TotalMilliseconds / span.TotalMilliseconds * ActualWidth;
                if (x < 0 || x > ActualWidth)
                    continue; // Skip ticks outside the bounds
                Pen pen = new Pen(Brushes.Gray, 0.5);
                dc.DrawLine(pen, new Point(x, ActualHeight), new Point(x, ActualHeight - 5));
            }

            // Draw major ticks
            t = AlignTime(TimeLine.StartTime, maj_interval);
            bool showDate = (maj_interval < TimeSpan.FromHours(6)) ? true : false;
            DateTime previousDate = t;
            for (; t <= TimeLine.EndTime; t += maj_interval)
            {
                double x = (t - TimeLine.StartTime).TotalMilliseconds / span.TotalMilliseconds * ActualWidth;
                if (x < 0 || x > ActualWidth)
                    continue; // Skip ticks outside the bounds

                Pen pen = new Pen(Brushes.Black, 1);
                dc.DrawLine(pen, new Point(x, ActualHeight), new Point(x, ActualHeight - 10));

                // Noon marker
                if ((t.Hour == 0) && t.Minute == 0 && t.Second == 0)
                {
                    dc.DrawLine(new Pen(Brushes.Red, 1), new Point(x, ActualHeight), new Point(x, ActualHeight - 10));
                }

                // Draw time label
                FormattedText timeLabel = new FormattedText(
                    t.ToString("HH:mm:ss"),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);

                double centerX = x - timeLabel.Width / 2;
                dc.DrawText(timeLabel, new Point(centerX, 14));


                // Draw date label if it has changed
                if ((t.Day - previousDate.Day) > 0 || (t.Month - previousDate.Month) > 0)
                {
                    showDate = true;
                }

                if (showDate)
                {
                    FormattedText dateLabel = new FormattedText(
                        t.ToString("yyyy-MM-dd"),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        12,
                        Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    double dateX = x - dateLabel.Width / 2;
                    dc.DrawText(dateLabel, new Point(dateX, 0));
                    previousDate = t;
                    showDate = false;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
