using RoSatGCS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace RoSatGCS.Controls
{
    public class CommandCollectionViewSource : CollectionViewSource
    {
        public static readonly DependencyProperty CommandGroupProperty = DependencyProperty.Register(
            "CommandGroup", typeof(ObservableCollection<SatelliteCommandGroupModel>), typeof(CommandCollectionViewSource),
            new PropertyMetadata(new ObservableCollection<SatelliteCommandGroupModel>(), OnCommandGroupChanged));

        private static void OnCommandGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((CommandCollectionViewSource)d).UpdateGroupNames();

        public ObservableCollection<SatelliteCommandGroupModel> CommandGroup
        {
            get => (ObservableCollection<SatelliteCommandGroupModel>)GetValue(CommandGroupProperty);
            set => SetValue(CommandGroupProperty, value);
        }

        public CommandCollectionViewSource()
        {
            DependencyPropertyDescriptor
                .FromProperty(ViewProperty, typeof(CommandCollectionViewSource))
                .AddValueChanged(this, OnViewChanged);
        }

        private void OnViewChanged(object? sender, EventArgs e) => UpdateGroupNames();

        private void OnViewCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateGroupNames();

        private void UpdateGroupNames()
        {
            if (View == null) return;
            if (View.GroupDescriptions.Count != 1)
                throw new Exception("CustomCollectionViewSource must have exactly one GroupDescription!");

            
            View.CollectionChanged -= OnViewCollectionChanged;
            try
            {
                var groupDescription = View.GroupDescriptions.First();
                //groupDescription.GroupNames.Clear();
                var names = View.Groups.Cast<CollectionViewGroup>().Select(cvg => cvg.Name.ToString());
                foreach (var s in CommandGroup.Select(o => o.Name).Except(names))
                    groupDescription.GroupNames.Add(s);
            }
            finally
            {
                View.CollectionChanged += OnViewCollectionChanged;
            }
        }
    }
}
