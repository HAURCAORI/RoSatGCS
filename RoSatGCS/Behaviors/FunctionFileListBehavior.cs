using Microsoft.Xaml.Behaviors;
using RoSatGCS.Models;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;

namespace RoSatGCS.Behaviors
{
    internal class FunctionFileListBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.Register(
            "DropCommand",
            typeof(ICommand),
            typeof(FunctionFileListBehavior),
            new PropertyMetadata(null));

        public ICommand DropCommand
        {
            get => (ICommand)GetValue(DropCommandProperty);
            set => SetValue(DropCommandProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.DragOver += OnDragOver;
            AssociatedObject.Drop += OnDrop;
        }


        protected override void OnDetaching()
        {
            AssociatedObject.DragOver -= OnDragOver;
            AssociatedObject.Drop -= OnDrop;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (DropCommand != null && DropCommand.CanExecute(files))
                {
                    DropCommand.Execute(files);
                }
            }
        }

        
    }
}
