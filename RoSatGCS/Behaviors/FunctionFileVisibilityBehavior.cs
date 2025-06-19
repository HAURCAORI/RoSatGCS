using Microsoft.Xaml.Behaviors;
using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RoSatGCS.Behaviors
{
    class FunctionFileVisibilityBehavior : Behavior<Button>
    {
        public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            "CommandParameter",
            typeof(object),
            typeof(FunctionFileVisibilityBehavior),
            new PropertyMetadata(null));
        public object CommandParameter
        {
            get => (object)GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            "Command",
            typeof(ICommand),
            typeof(FunctionFileVisibilityBehavior),
            new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.Click += OnClick;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Click -= OnClick;
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            
            if (Command != null && CommandParameter != null && Command.CanExecute(CommandParameter))
            {
                if(CommandParameter is SatelliteFunctionFileModel model)
                {
                    model.Visibility = !model.Visibility;
                    Command.Execute(CommandParameter);
                }
            }
        }
    }
}

