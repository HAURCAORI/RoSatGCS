using FileListView.Interfaces;
using Microsoft.Xaml.Behaviors;
using RoSatGCS.Utils.Converter;
using RoSatGCS.Utils.Files;
using RoSatGCS.Utils.Localization;
using RoSatGCS.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RoSatGCS.Behaviors
{
    internal class FileLocalDragDropBehavior : Behavior<ListView>
    {
        public static readonly DependencyProperty ParentProperty =
        DependencyProperty.Register(
            nameof(Parent),
            typeof(PageFileShareViewModel),
            typeof(FileLocalDragDropBehavior),
            new PropertyMetadata(null));

        public PageFileShareViewModel Parent
        {
            get => (PageFileShareViewModel)GetValue(ParentProperty);
            set => SetValue(ParentProperty, value);
        }
        private Page? _adornerRoot => FindParent<Page>(AssociatedObject);

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AllowDrop = true;
            AssociatedObject.DragEnter += OnDragOverOrEnter;
            AssociatedObject.DragOver += OnDragOverOrEnter;
            AssociatedObject.Drop += OnDrop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.DragEnter -= OnDragOverOrEnter;
            AssociatedObject.DragOver -= OnDragOverOrEnter;
            AssociatedObject.Drop -= OnDrop;
            base.OnDetaching();
        }

        private void OnDragOverOrEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FileDragDropBehavior.FileDragWrapper)))
            {
                e.Effects = DragDropEffects.Move; // shows move cursor over target
                e.Handled = true;
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(FileDragDropBehavior.FileDragWrapper)))
                return;

            var adornerLayer = AdornerLayer.GetAdornerLayer(_adornerRoot);
            var adornersOfStackPanel = adornerLayer.GetAdorners(_adornerRoot);

            foreach (var adorner in adornersOfStackPanel)
                adornerLayer.Remove(adorner);

            var items = e.Data.GetData(typeof(FileDragDropBehavior.FileDragWrapper));
            if (items is not FileDragDropBehavior.FileDragWrapper wrapper)
                return;

            var item = wrapper.Items.FirstOrDefault();
            if (item == null) { return; }

            if(item.ItemType != FileSystemModels.Models.FSItems.Base.FSItemType.File)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Only files can be uploaded.", "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            long size = 0;
            if(FileSizeHelper.TryGetFileSize(item.ItemPath, out size, out _) == false || size == 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                     MessageBox.Show("Invalid file size.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;
            }

            if(size > 1024*1024*100)
            {
                MessageBoxResult result = await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show("File size is too big.", TranslationSource.Instance["zAreYouSure"],
                MessageBoxButton.OKCancel, MessageBoxImage.Question));
                if (result != MessageBoxResult.OK)
                    return;
            }

            Parent.FileUploadCommand.Execute(item.ItemPath);
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

    }
}
