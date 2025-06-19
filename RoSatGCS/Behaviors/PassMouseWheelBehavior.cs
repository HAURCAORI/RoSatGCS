using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace RoSatGCS.Behaviors
{
    public static class PassMouseWheelBehavior
    {

        public static bool GetEnableMouseWheelPass(DependencyObject obj) => (bool)obj.GetValue(EnableMouseWheelPassProperty);
        public static void SetEnableMouseWheelPass(DependencyObject obj, bool value) => obj.SetValue(EnableMouseWheelPassProperty, value);

        public static readonly DependencyProperty EnableMouseWheelPassProperty =
            DependencyProperty.RegisterAttached(
                "EnableMouseWheelPass",
                typeof(bool),
                typeof(PassMouseWheelBehavior),
                new UIPropertyMetadata(false, OnEnableMouseWheelPassChanged));

        private static void OnEnableMouseWheelPassChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                    element.PreviewMouseWheel += OnPreviewMouseWheel;
                else
                    element.PreviewMouseWheel -= OnPreviewMouseWheel;
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sen = sender as DependencyObject;
            var src = e.OriginalSource as DependencyObject;
            if (sen == null || src == null) return;
            
            var ctrl = FindVisualParent<UserControl>(src);
            var menu = FindVisualParent<ComboBoxItem>(src);
            var scrollViewer = FindVisualParent<ScrollViewer>(sen);
            if(scrollViewer == null) return;

            if (ctrl != null || menu != null) return;

            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
            e.Handled = true;
        }

        private static T FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
