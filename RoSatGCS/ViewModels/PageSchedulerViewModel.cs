using GCSControls;
using RoSatGCS.Utils.Satellites.Core;
using RoSatGCS.Utils.Satellites.Observation;
using RoSatGCS.Utils.Satellites.TLE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.ViewModels
{
    public class PageSchedulerViewModel : ViewModelPageBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ObservableCollection<PaneViewModel> _anchorable = [];
        private readonly ObservableCollection<PaneViewModel> _document = [];
        private PaneViewModel? _activeDocument;
        public ObservableCollection<PaneViewModel> Anchorable { get => _anchorable; }
        public ObservableCollection<PaneViewModel> DocumentPane { get => _document; }
        public PaneViewModel? ActiveDocument
        {
            get => _activeDocument;
            set
            {
                if (_activeDocument != value)
                {
                    SetProperty(ref _activeDocument, value);
                }
            }
        }

        private readonly WeakReference<PaneTleListViewModel> _paneTleList;
        public PaneTleListViewModel? PaneTleList { get => _paneTleList?.TryGetTarget(out var target) == true ? target : null; }

        private readonly WeakReference<PanePassCommandViewModel> _panePassCommand;
        public PanePassCommandViewModel? PanePassCommand { get => _panePassCommand?.TryGetTarget(out var target) == true ? target : null; }

        private readonly WeakReference<PanePassQueueViewModel> _panePassQueue;
        public PanePassQueueViewModel? PanePassQueue { get => _panePassQueue?.TryGetTarget(out var target) == true ? target : null; }

        private readonly WeakReference<PanePassScheduleViewModel> _panePassSchedule;
        public PanePassScheduleViewModel? PanePassSchedule { get => _panePassSchedule?.TryGetTarget(out var target) == true ? target : null; }

        private readonly WeakReference<PanePassTimelineViewModel> _panePassTimeline;
        public PanePassTimelineViewModel? PanePassTimeline { get => _panePassTimeline?.TryGetTarget(out var target) == true ? target : null; }






        private ObservableCollection<TimeSlotItem> _slots = new ObservableCollection<TimeSlotItem>();

        public ObservableCollection<TimeSlotItem> TimeSlotList
        {
            get => _slots;
        }

        public PageSchedulerViewModel()
        {
            _paneTleList = new WeakReference<PaneTleListViewModel>(new PaneTleListViewModel());
            if (PaneTleList != null)
                _anchorable.Add(PaneTleList);

            _panePassQueue = new WeakReference<PanePassQueueViewModel>(new PanePassQueueViewModel());
            if (PanePassQueue != null)
                _document.Add(PanePassQueue);

            _panePassSchedule = new WeakReference<PanePassScheduleViewModel>(new PanePassScheduleViewModel());
            if (PanePassSchedule != null)
                _document.Add(PanePassSchedule);

            _panePassCommand = new WeakReference<PanePassCommandViewModel>(new PanePassCommandViewModel());
            if (PanePassCommand != null)
                _document.Add(PanePassCommand);

            _panePassTimeline = new WeakReference<PanePassTimelineViewModel>(new PanePassTimelineViewModel());
            if (PanePassTimeline != null)
                _anchorable.Add(PanePassTimeline);

            

        }

        public void Init()
        {

                var tle1 = "ISS (ZARYA)";
                var tle2 = "1 25544U 98067A   19034.73310439  .00001974  00000-0  38215-4 0  9991";
                var tle3 = "2 25544  51.6436 304.9146 0005074 348.4622  36.8575 15.53228055154526";
                var tle = new TLE(tle1, tle2, tle3);

                var sat = new Satellite(tle);

                var location = new GeoCoordinate(Angle.FromDegrees(40.689236), Angle.FromDegrees(-74.044563), 0);
                var ground = new GroundStation(location);

                var start = new Julian(DateTime.Now);
                var end = new Julian(DateTime.Now.AddDays(2));
                var delta = new TimeSpan(0, 1, 0);
                var observations = ground.Observe(sat, start, end, delta, resolution: 4);

                for (var i = 0; i < observations.Count; i++)
                {
                    var observation = observations[i];
                    TimeSlotPriority priority;
                    if (observation.MaxElevation < 20)
                    {
                        priority = TimeSlotPriority.Low;
                    }
                    else if (observation.MaxElevation < 40)
                    {
                        priority = TimeSlotPriority.Mid;
                    }
                    else
                    {
                        priority = TimeSlotPriority.High;
                    }
                    _slots.Add(new TimeSlotItem("ISS[" + i.ToString() + "]", observation.Start.ToTime(), observation.End.ToTime(), priority));
                }

        }
    }
}
