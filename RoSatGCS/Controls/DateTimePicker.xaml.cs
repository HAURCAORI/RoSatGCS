using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace RoSatGCS.Controls
{
    public partial class DateTimePicker : UserControl
    {
        private const string DateTimeFormat = "yyyy.MM.dd HH:mm";
        
        public ObservableCollection<int> HourList { get; set; }
        public ObservableCollection<int> MinuteList { get; set; }

        public DateTimePicker()
        {
            InitializeComponent();
            HourList = new ObservableCollection<int>();
            MinuteList = new ObservableCollection<int> { 0, 10, 20, 30, 40, 50 };

            for (int i = 0; i < 24; i++) HourList.Add(i);

            MinuteSelector.SelectedIndex = 0;
            HourSelector.SelectedIndex = 0;

            DataContext = this;

            CalDisplay.SelectedDatesChanged += OnCalendarChanged;
            CalDisplay.SelectedDate = DateTime.Now.Date;
            HourSelector.SelectionChanged += OnTimeChanged;
            MinuteSelector.SelectionChanged += OnTimeChanged;

            UpdateSelectedDate();
        }

        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime),
                typeof(DateTimePicker), new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DateTime SelectedDate
        {
            get => (DateTime)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        private void OnCalendarChanged(object? sender, SelectionChangedEventArgs e) => UpdateSelectedDate();

        private void OnTimeChanged(object? sender, SelectionChangedEventArgs e) => UpdateSelectedDate();

        private void SaveTime_Click(object? sender, RoutedEventArgs e)
        {
            UpdateSelectedDate();
            PopUpCalendarButton.IsChecked = false;
        }

        private void UpdateSelectedDate()
        {
            if (CalDisplay.SelectedDate == null) return;

            int hour = HourSelector.SelectedItem is int h ? h : 0;
            int minute = MinuteSelector.SelectedItem is int m ? m : 0;

            DateTime date = CalDisplay.SelectedDate.Value.Date + new TimeSpan(hour, minute, 0);

            SelectedDate = date;
            DateDisplay.Text = date.ToString(DateTimeFormat);
        }

        private void CalDisplay_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured is CalendarItem) Mouse.Capture(null);
        }
    }

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && !b;
        }
    }
}