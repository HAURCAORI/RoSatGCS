using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace RoSatGCS.Behaviors
{
    public enum NumericInputMode
    {
        None,
        UInt8,
        UInt16,
        UInt32,
        UInt64,
    }

    public static class TextBoxModeBehavior
    {
        public static readonly DependencyProperty NumericModeProperty =
        DependencyProperty.RegisterAttached(
            "NumericMode",
            typeof(NumericInputMode),
            typeof(TextBoxModeBehavior),
            new PropertyMetadata(NumericInputMode.None, OnNumericModeChanged));

        public static NumericInputMode GetNumericMode(DependencyObject obj) =>
            (NumericInputMode)obj.GetValue(NumericModeProperty);

        public static void SetNumericMode(DependencyObject obj, NumericInputMode value) =>
            obj.SetValue(NumericModeProperty, value);

        private static void OnNumericModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((NumericInputMode)e.NewValue != NumericInputMode.None)
                {
                    textBox.PreviewTextInput += OnPreviewTextInput;
                    DataObject.AddPastingHandler(textBox, OnPaste);
                }
                else
                {
                    textBox.PreviewTextInput -= OnPreviewTextInput;
                    DataObject.RemovePastingHandler(textBox, OnPaste);
                }
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var proposedText = GetProposedText(textBox, e.Text);
                e.Handled = !IsValidNumber(proposedText, GetNumericMode(textBox));
            }
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is TextBox textBox && e.DataObject.GetDataPresent(typeof(string)))
            {
                var pastedText = (string)e.DataObject.GetData(typeof(string))!;
                var proposedText = GetProposedText(textBox, pastedText);

                if (!IsValidNumber(proposedText, GetNumericMode(textBox)))
                    e.CancelCommand();
            }
        }

        private static string GetProposedText(TextBox textBox, string input)
        {
            var realText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength);
            return realText.Insert(textBox.SelectionStart, input);
        }

        private static bool IsValidNumber(string text, NumericInputMode mode)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!text.All(char.IsDigit)) return false;

            return mode switch
            {
                NumericInputMode.UInt8 => byte.TryParse(text, out _),
                NumericInputMode.UInt16 => ushort.TryParse(text, out _),
                NumericInputMode.UInt32 => uint.TryParse(text, out _),
                NumericInputMode.UInt64 => ulong.TryParse(text, out _),
                _ => false
            };

        }
    }
}
