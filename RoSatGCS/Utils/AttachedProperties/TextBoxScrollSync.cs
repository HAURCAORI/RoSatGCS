using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace RoSatGCS.Utils.AttachedProperties
{
    public class TextBoxScrollSync
    {
        public static readonly DependencyProperty SyncTargetProperty =
       DependencyProperty.RegisterAttached(
           "SyncTarget",
           typeof(TextBox),
           typeof(TextBoxScrollSync),
           new PropertyMetadata(null, OnSyncTargetChanged));

        public static void SetSyncTarget(DependencyObject obj, TextBox value)
            => obj.SetValue(SyncTargetProperty, value);

        public static TextBox GetSyncTarget(DependencyObject obj)
            => (TextBox)obj.GetValue(SyncTargetProperty);

        // Internal property to store the event handler
        private static readonly DependencyProperty ScrollChangedHandlerProperty =
            DependencyProperty.RegisterAttached(
                "ScrollChangedHandler",
                typeof(ScrollChangedEventHandler),
                typeof(TextBoxScrollSync),
                new PropertyMetadata(null));

        private static void SetScrollChangedHandler(DependencyObject element, ScrollChangedEventHandler? handler) =>
            element.SetValue(ScrollChangedHandlerProperty, handler);

        private static ScrollChangedEventHandler? GetScrollChangedHandler(DependencyObject element) =>
            element.GetValue(ScrollChangedHandlerProperty) as ScrollChangedEventHandler;

        private static void OnSyncTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox follower)
                return;

            // Unsubscribe old handler
            if (e.OldValue is TextBox oldLeader)
            {
                var oldSV = GetScrollViewer(oldLeader);
                var oldHandler = GetScrollChangedHandler(follower);
                if (oldSV != null && oldHandler != null)
                {
                    oldSV.ScrollChanged -= oldHandler;
                    SetScrollChangedHandler(follower, null);
                }
            }

            // Subscribe new handler
            if (e.NewValue is TextBox newLeader)
            {
                var newSV = GetScrollViewer(newLeader);
                if (newSV != null)
                {
                    ScrollChangedEventHandler handler = (s, args) =>
                    {
                        var followerSV = GetScrollViewer(follower);
                        if (followerSV != null)
                        {
                            followerSV.ScrollToHorizontalOffset(newSV.HorizontalOffset);
                            followerSV.ScrollToVerticalOffset(newSV.VerticalOffset);
                        }
                    };

                    newSV.ScrollChanged += handler;
                    SetScrollChangedHandler(follower, handler);
                }
            }
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject element)
        {
            if (element == null) return null;

            if (element is ScrollViewer sv)
                return sv;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
