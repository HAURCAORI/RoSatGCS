using FileListView.Interfaces;
using Microsoft.Xaml.Behaviors;
using RoSatGCS.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(FileDragDropBehavior.FileDragWrapper)))
                return;

            var items = e.Data.GetData(typeof(FileDragDropBehavior.FileDragWrapper));

            // TODO: Implement the logic to handle the dropped items
        }

    }
}
