using AvalonDock.Layout;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RoSatGCS.Behaviors
{

    public class AutoScrollBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(
                nameof(IsEnabled),
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(true));

        /// <summary>
        /// Bind this to your AutoScroll checkbox (e.g. AutoScrollEnabled).
        /// </summary>
        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject is ListBox lb)
            {
                lb.MouseLeave += LostFocus;
            }

            if (AssociatedObject.Items is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnItemsCollectionChanged;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject is ListBox lb)
            {
                lb.MouseLeave -= LostFocus;
            }
            if (AssociatedObject.Items is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= OnItemsCollectionChanged;
            }

            base.OnDetaching();
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Only care about adds
            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                TryScrollToBottom();
            }
        }

        private void TryScrollToBottom()
        {
            if (!IsEnabled)
                return;

            if (AssociatedObject.Items.Count == 0)
                return;

            AssociatedObject.Dispatcher.InvokeAsync(() =>
            {
                var lastItem = AssociatedObject.Items[AssociatedObject.Items.Count - 1];
                AssociatedObject.ScrollIntoView(lastItem);
            });
        }

        private void LostFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is ListBox lb)
            {
                lb.SelectedItem = null;
            }
        }
    }
}
