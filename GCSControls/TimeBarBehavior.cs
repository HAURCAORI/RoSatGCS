using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace GCSControls
{
    public class TimeBarBehavior : Behavior<FrameworkElement>
    {
        private bool _isPanning;
        private Point _lastPos;


        public static readonly DependencyProperty TimeLineProperty =
            DependencyProperty.Register(nameof(TimeLine), typeof(TimeLine), typeof(TimeBarBehavior), new PropertyMetadata(null));

        public TimeLine TimeLine
        {
            get => (TimeLine)GetValue(TimeLineProperty);
            set => SetValue(TimeLineProperty, value);
        }


        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
            AssociatedObject.PreviewMouseLeftButtonUp += OnMouseUp;
            AssociatedObject.PreviewMouseMove += OnMouseMove;
            AssociatedObject.MouseLeave += OnMouseLeave;
            AssociatedObject.PreviewMouseWheel += OnMouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseDown;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnMouseUp;
            AssociatedObject.PreviewMouseMove -= OnMouseMove;
            AssociatedObject.MouseLeave -= OnMouseLeave;
            AssociatedObject.PreviewMouseWheel -= OnMouseWheel;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPanning = true;
            _lastPos = e.GetPosition(sender as IInputElement);
            AssociatedObject.CaptureMouse();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            AssociatedObject.ReleaseMouseCapture();
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            _isPanning = false; 
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning || e.LeftButton != MouseButtonState.Pressed)
                return;

            var current = e.GetPosition(sender as IInputElement);

            double dx = current.X - _lastPos.X;
            TimeSpan span = TimeLine.Interval;
            double pixelsPerSecond = AssociatedObject.ActualWidth / span.TotalSeconds;
            double delta = -dx / pixelsPerSecond;

            TimeLine.Shift(TimeSpan.FromSeconds(delta));

            _lastPos = current;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (TimeLine == null || AssociatedObject.ActualWidth == 0)
                return;
            double factor = e.Delta > 0 ? 0.9 : 1.1;
            Point position = e.GetPosition(sender as IInputElement);
            double ratio = position.X / AssociatedObject.ActualWidth;
            TimeSpan span = TimeLine.Interval;
            DateTime center = TimeLine.StartTime + TimeSpan.FromTicks((long)(TimeLine.Interval.Ticks * ratio));
            TimeLine.Zoom(center, factor, ratio);
        }
    }
}
