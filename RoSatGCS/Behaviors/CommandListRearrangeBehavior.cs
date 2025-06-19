using Microsoft.Xaml.Behaviors;
using RoSatGCS.Controls;
using RoSatGCS.Models;
using RoSatGCS.Utils.Localization;
using RoSatGCS.ViewModels;
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
using System.Windows.Media;

namespace RoSatGCS.Behaviors
{
    public class CommandListRearrangeBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty ParentProperty =
       DependencyProperty.Register(
           "Parent",
           typeof(PageCommandViewModel),
           typeof(CommandListRearrangeBehavior),
           new PropertyMetadata(null));
        public PageCommandViewModel Parent
        {
            get => (PageCommandViewModel)GetValue(ParentProperty);
            set => SetValue(ParentProperty, value);
        }

        private struct DragWrapper{
            public SatelliteCommandModel Last;
            public List<SatelliteCommandModel> Items;
            public ListBox Source;
        }

        private Point _dragStartPoint;

        private DragAdorner? _dragAdorner;
        private DropInsertionAdorner? _insertionAdorner;
        private AdornerLayer? _adornerLayer;
        

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.GiveFeedback += OnGiveFeedback;
            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove += OnMouseMove;
            AssociatedObject.AllowDrop = true;
            AssociatedObject.Drop += OnDrop;
            AssociatedObject.DragOver += OnDragOver;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.GiveFeedback -= OnGiveFeedback;
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove -= OnMouseMove;
            AssociatedObject.Drop -= OnDrop;
            AssociatedObject.DragOver -= OnDragOver;
        }

        private void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (_adornerLayer == null) return;
            if (e.Effects == DragDropEffects.None)
                _adornerLayer.Visibility = Visibility.Hidden;
            else if (e.Effects == DragDropEffects.Move)
                _adornerLayer.Visibility = Visibility.Visible;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not UIElement sourceElement)
                return;

            var item = FindParent<ListBoxItem>(sourceElement);
            Point mousePos = e.GetPosition(null);

            if (item?.IsSelected == true && item?.DataContext is not SatelliteCommandGroupModel)
            {
                Vector diff = _dragStartPoint - mousePos;
                if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
                {
                    e.Handled = true;
                }
            }

            _dragStartPoint = mousePos;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ListBox root) return;
            if (e.OriginalSource is not UIElement sourceElement) return;

            var listBox = FindParent<ListBox>(sourceElement);
            if (listBox == null) { return; }

            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                
                var selectedItems = listBox.SelectedItems.OfType<SatelliteCommandModel>().ToList();
                if (selectedItems.Count == 0) return;
                if (listBox.ItemsSource is not System.Collections.IList itemsSource) return;

                // Draw Adorner
                _adornerLayer ??= AdornerLayer.GetAdornerLayer(root);

                var container = FindParent<ListBoxItem>(sourceElement);
                if (container == null) return;

                ClearAdorners();

                _dragAdorner = new DragAdorner(root, container);
                _insertionAdorner ??= new DropInsertionAdorner(root);

                _adornerLayer?.Add(_dragAdorner);
                _adornerLayer?.Add(_insertionAdorner);

                // Start Drag
                try
                {
                    var wrapper = new DragWrapper
                    {
                        Last = selectedItems.Last(),
                        Items = selectedItems.OrderBy(item => itemsSource.IndexOf(item)).ToList(),
                        Source = listBox
                    };

                    DragDrop.DoDragDrop(root, wrapper, DragDropEffects.Move);
                }
                catch
                {
                    ClearAdorners();
                }
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            _dragAdorner?.UpdatePosition(e.GetPosition((UIElement)sender));

            if (_insertionAdorner == null) return;

            if (e.OriginalSource is not UIElement sourceElement)
                return;

            var listBox = FindParent<ListBox>(sourceElement);
            var listBoxItem = FindParent<ListBoxItem>(sourceElement);
            if (listBox == null || listBoxItem == null)
                return;

            if(listBoxItem.DataContext is not SatelliteCommandModel)
                return;

            if (listBox.ItemsSource is not System.Collections.IList itemsSource)
                return;

            if (e.Data.GetData(typeof(DragWrapper)) is not DragWrapper wrapper)
                return;

            bool insertAbove = false;
            if (listBox == wrapper.Source) {
                int targetIndex = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);
                insertAbove = itemsSource.IndexOf(wrapper.Last) >= targetIndex;
            }

            _insertionAdorner.Show(listBoxItem, insertAbove);
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            ClearAdorners();

            if (e.OriginalSource is not UIElement sourceElement)
                return;

            var listBox = FindParent<ListBox>(sourceElement);
            var listBoxItem = FindParent<ListBoxItem>(sourceElement);
            if (listBox == null || listBoxItem == null)
                return;

            if (listBoxItem.DataContext is not SatelliteCommandModel || listBoxItem.IsSelected)
                return;

            if (listBox.ItemsSource is not System.Collections.IList itemsTarget) 
                return;

            if (e.Data.GetData(typeof(DragWrapper)) is not DragWrapper wrapper)
                return;


            int targetIndex = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);

            if (itemsTarget.IndexOf(wrapper.Last) < targetIndex)
            {
                wrapper.Items.Reverse();
            }

            if (listBox != wrapper.Source)
            {
                var targetModels = itemsTarget.OfType<SatelliteCommandModel>().ToList();
                foreach (var item in wrapper.Items)
                {
                    if (targetModels.FirstOrDefault(o => o.Name == item.Name) != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show(TranslationSource.Instance["zCommandAlreadyExists"] + "\r\n" + item.Name, TranslationSource.Instance["sWarning"],
                        MessageBoxButton.OK, MessageBoxImage.Warning));
                        return;
                    }
                        
                }
            }

            var targetGroupList = FindParent<ListBoxItem>(listBox);
            if (targetGroupList == null)
                return;

            if (targetGroupList.DataContext is not SatelliteCommandGroupModel targetGroupModel)
                return;

            var sourceGroupList = FindParent<ListBoxItem>(wrapper.Source);
            if (sourceGroupList == null)
                return;

            if (sourceGroupList.DataContext is not SatelliteCommandGroupModel sourceGroupModel)
                return;


            foreach (var item in wrapper.Items)
            {
                if (listBox == wrapper.Source)
                {
                    targetGroupModel.Reorder(item, targetIndex);
                    targetIndex = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);
                }
                else
                {
                    if (Parent == null)
                        return;

                    var ret = targetGroupModel.Add(item);
                    if (ret == null)
                        return;

                    Parent.FindFunctionPropertyPane(item)?.Close.Execute(null);

                    sourceGroupModel.DeleteCommandAndDispose(item);

                    
                    targetGroupModel.Reorder(ret, targetIndex + 1);
                    targetIndex = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);
                }
            }
        }

        private static T? FindParent<T>(UIElement? child) where T : DependencyObject
        {
            if (child == null)
                return null;

            if (VisualTreeHelper.GetParent(child) is not UIElement parent)
                return null;

            if (parent is T typedParent)
                return typedParent;

            return FindParent<T>(parent);
        }

        private static ListBoxItem? GetNearestContainer(UIElement? element)
        {
            while (element != null && !(element is ListBoxItem))
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
            }
            return element as ListBoxItem;
        }


        private void ClearAdorners()
        {
            if (_dragAdorner != null)
            {
                _adornerLayer?.Remove(_dragAdorner);
                _dragAdorner = null;
            }
            if (_insertionAdorner != null)
            {
                _adornerLayer?.Remove(_insertionAdorner);
                _insertionAdorner = null;
            }
        }
    }
}
