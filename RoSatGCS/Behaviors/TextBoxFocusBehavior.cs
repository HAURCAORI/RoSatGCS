using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;

namespace RoSatGCS.Behaviors
{
    public static class TextBoxFocusBehavior
    {
        public static bool GetIsFocused(DependencyObject obj) => (bool)obj.GetValue(IsFocusedProperty);
        public static void SetIsFocused(DependencyObject obj, bool value) => obj.SetValue(IsFocusedProperty, value);

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(TextBoxFocusBehavior),
                new PropertyMetadata(false, OnIsFocusedChanged));

        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox && (bool)e.NewValue)
            {
                if((bool) e.NewValue == false) { return; }

                textBox.IsVisibleChanged += TextBox_IsVisibleChanged;

            }
        }

        private static void TextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                textBox.Focus();
                textBox.CaretIndex = textBox.Text.Length;
            }, System.Windows.Threading.DispatcherPriority.Render);

            // Unsubscribe to avoid repeated calls
            textBox.IsVisibleChanged -= TextBox_IsVisibleChanged;
        }
    }
}
