using Microsoft.Xaml.Behaviors;
using RoSatGCS.Models;
using RoSatGCS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace RoSatGCS.Behaviors
{
    public class CommandListDoubleClickBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty ParentProperty =
       DependencyProperty.Register(
           "Parent",
           typeof(PageCommandViewModel),
           typeof(CommandListDoubleClickBehavior),
           new PropertyMetadata(null));
        public PageCommandViewModel Parent
        {
            get => (PageCommandViewModel)GetValue(ParentProperty);
            set => SetValue(ParentProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseDoubleClick += OnDoubleClick;
        }


        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseDoubleClick -= OnDoubleClick;
            
        }

        private void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Parent == null) return;
            if (AssociatedObject.SelectedItem == null) return;

            AssociatedObject.PreviewMouseLeftButtonUp += OnMouseLeftUp;
        }

        private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.SelectedItem is SatelliteCommandModel s)
                Parent.OpenFunctionPropertyPane(s);
            else if (AssociatedObject.SelectedItem is SatelliteMethodModel m)
                Parent.OpenFunctionPropertyPane(m);
            AssociatedObject.PreviewMouseLeftButtonUp -= OnMouseLeftUp;
        }
    }
}
