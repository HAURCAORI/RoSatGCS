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
    public partial class DateTimePickerSecond : UserControl
    {
        private const string DateTimeFormat = "yyyy.MM.dd HH:mm:ss";
        
        public ObservableCollection<int> HourList { get; set; }
        public ObservableCollection<int> MinuteList { get; set; }
        public ObservableCollection<int> SecondList { get; set; }


        public DateTimePickerSecond()
        {
            InitializeComponent();
            HourList = new ObservableCollection<int>();
            MinuteList = new ObservableCollection<int>();
            SecondList = new ObservableCollection<int>();

            for (int i = 0; i < 24; i++) HourList.Add(i);
            for (int I = 0; I < 60; I++) { MinuteList.Add(I); SecondList.Add(I); }

            SecondSelector.SelectedIndex = 0;
            MinuteSelector.SelectedIndex = 0;
            HourSelector.SelectedIndex = 0;

            //DataContext = this;
            HourSelector.ItemsSource = HourList;
            MinuteSelector.ItemsSource = MinuteList;
            SecondSelector.ItemsSource = SecondList;


            CalDisplay.SelectedDatesChanged += OnCalendarChanged;
            CalDisplay.SelectedDate = DateTime.UtcNow.Date;
            HourSelector.SelectionChanged += OnTimeChanged;
            MinuteSelector.SelectionChanged += OnTimeChanged;
            SecondSelector.SelectionChanged += OnTimeChanged;

            UpdateSelectedDate();
        }

        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime),
                typeof(DateTimePickerSecond), new FrameworkPropertyMetadata(DateTime.UtcNow, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

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
            int second = SecondSelector.SelectedItem is int s ? s : 0;

            DateTime date = CalDisplay.SelectedDate.Value.Date + new TimeSpan(hour, minute, second);

            SelectedDate = date;
            DateDisplay.Text = date.ToString(DateTimeFormat);
        }

        private void CalDisplay_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured is CalendarItem) Mouse.Capture(null);
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (DateTimePickerSecond)d;

            var incoming = (DateTime)e.NewValue;
            // Treat Unspecified as UTC (since your VM sends UTC)
            var utc = incoming.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(incoming, DateTimeKind.Utc)
                        : incoming.ToUniversalTime();

            //ctrl.CalDisplay.SelectedDate = utc.Date;

            ctrl.DateDisplay.Text = utc.ToString(DateTimeFormat);
        }
    }
}