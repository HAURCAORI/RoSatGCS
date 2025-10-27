using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RoSatGCS.Models
{
    public sealed class BoundedSortedObservableCollection<T> : ObservableCollection<T>
    {
        private readonly Func<T, DateTime> _keySelector;
        public int Capacity { get; }

        public BoundedSortedObservableCollection(int capacity, Func<T, DateTime> keySelector)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }
        public void AddSorted(T item)
        {
            // must mutate on UI thread
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(() => AddSorted(item));
                return;
            }

            var dt = _keySelector(item);

            // Fast path: append if >= last
            if (Count == 0 || _keySelector(this[Count - 1]) <= dt)
            {
                base.InsertItem(Count, item);
            }
            else
            {
                // Binary-search insert position
                int idx = BinarySearch(dt);
                base.InsertItem(idx, item);
            }

            // Enforce capacity: drop oldest (index 0) until Count <= Capacity
            while (Count > Capacity)
                base.RemoveItem(0);
        }

        public void AddRangeAssumingSorted(ReadOnlySpan<T> items)
        {
            if (items.Length == 0) return;

            // If the first is out of order (earlier than last), fall back to individual AddSorted
            if (Count > 0 && _keySelector(items[0]) < _keySelector(this[Count - 1]))
            {
                foreach (var it in items) AddSorted(it);
                return;
            }

            // Fast append
            foreach (var it in items) base.InsertItem(Count, it);

            // Trim head if needed
            int toRemove = Count - Capacity;
            while (toRemove-- > 0) base.RemoveItem(0);
        }

        // Standard binary search for first index with key > desired? No, we want first >= dt.
        private int BinarySearch(DateTime dt)
        {
            int lo = 0, hi = Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int cmp = _keySelector(this[mid]).CompareTo(dt);
                if (cmp < 0) lo = mid + 1;
                else hi = mid - 1;
            }
            // lo = first index with key >= dt (stable insert)
            return lo;
        }
    }

    public class PlotDataContainer : INotifyPropertyChanged
    {
        private static readonly int DefaultCapacity = 10;
        private readonly ConcurrentDictionary<ushort, BoundedSortedObservableCollection<PlotData>> _map
        = new();

        public BoundedSortedObservableCollection<PlotData> GetSeries(ushort dataId)
        => _map.GetOrAdd(dataId, _ => new BoundedSortedObservableCollection<PlotData>(DefaultCapacity, p => p.DateTime));

        public BoundedSortedObservableCollection<PlotData> this[ushort dataId]
       => GetSeries(dataId);

        public void Add(ushort dataId, ReadOnlySpan<PlotData> items)
        {
            GetSeries(dataId).AddRangeAssumingSorted(items);
            OnPropertyChanged(nameof(this.Count));
        }

        public void Clear()
            => _map.Clear();


        public int Count => _map.Count;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
