using RoSatGCS.Utils.Satellites.Observation;
using RoSatGCS.Utils.Satellites.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Utils.Satellites.Observation
{
    public class GroundStation
    {
        public Coordinate Location { get; }
        public GroundStation(Coordinate location)
        {
            Location = location;
        }

        public List<SatelliteVisibilityPeriod> Observe(
            Satellite satellite,
            Julian start,
            Julian end,
            TimeSpan deltaTime,
            Angle minElevation = default,
            bool clipToStartTime = true, // default is true as it is assumed typical use case will be for future propagation, not searching into the past
            bool clipToEndTime = false, // default is false as it is assumed typical use case will be to capture entire future pass
            int resolution = 3
            )
        {
            // Validation checks
            if (deltaTime.TotalSeconds <= 0)
                throw new ArgumentException("deltaTime must be positive", nameof(deltaTime));

            if ((end - start).TotalSeconds < 0)
                throw new ArgumentException("endTime must be greater than startTime", nameof(end));

            if (deltaTime <= TimeSpan.Zero)
                throw new ArgumentException("deltaTime must be greater than zero", nameof(deltaTime));

            if (resolution < 0)
                throw new ArgumentException("resolution must be non-negative", nameof(resolution));

            if (resolution > 7)
                throw new ArgumentException("resolution must be no more than 7 decimal places (no more than tick resolution)", nameof(resolution));

            if (minElevation.Degrees > 90)
                throw new ArgumentException("minElevation cannot be greater than 90°", nameof(minElevation));


            start = start.Round(deltaTime);
            var clippedEnd = clipToEndTime ? end : null;

            var obs = new List<SatelliteVisibilityPeriod>();

            Julian aosTime;
            var t = start;

            do
            {
                // find the AOS Time of the next pass
                var aosCrossingPoint = FindNextBelowToAboveCrossingPoint(satellite, t, end, deltaTime, minElevation, resolution);
                if (aosCrossingPoint is null)
                    // we're done if no crossing point was found
                    break;

                aosTime = aosCrossingPoint;
                t = aosTime + deltaTime;

                // find the LOS time and max elevation for the next pass
                Julian losTime;
                Julian maxElTime;
                if (clippedEnd is not null && t > clippedEnd)
                {
                    losTime = clippedEnd;
                    maxElTime = clippedEnd;
                }
                else
                {
                    var tu = FindNextAboveToBelowCrossingPoint(satellite, t, deltaTime, minElevation, resolution, clippedEnd);
                    losTime = tu.CrossingPointTime;
                    maxElTime = tu.MaxElevationTime;
                }

                // if the LOS time is before the AOS time, skip this pass
                if (maxElTime == Julian.MinValue)
                {
                    t = losTime + deltaTime;
                    continue;
                }

                var before = maxElTime - deltaTime;

                if (clipToStartTime) // ensure before is clipped for max elevation search 
                {
                    before = start > before ? start : before;
                }

                var after = maxElTime + deltaTime;
                if (clipToEndTime) // ensure after is clipped for max elevation search
                {
                    after = end < after ? end : after;
                }

                // add the visibility period for the pass
                var refinedMaxElResult = FindMaxElevation(satellite, before, maxElTime, after, resolution);
                var maxEl = refinedMaxElResult.Item1;
                maxElTime = refinedMaxElResult.Item2;
                obs.Add(new SatelliteVisibilityPeriod(satellite, aosTime, losTime, maxEl, maxElTime, Location));

                t = losTime + deltaTime;
            } while (t <= end);

            // if clipToStartTime is false and the start time has been clipped, walk back in time until previous AOS crossing point has been found
            if (!clipToStartTime && obs.Count > 0 && obs[0].Start <= start)
            {
                var first = obs[0];
                var tu = FindNextAboveToBelowCrossingPoint(satellite, first.Start, deltaTime.Negate(), minElevation, resolution);
                var maxElTime = first.MaxElevation > tu.MaxElevation ? first.MaxElevationTime : tu.MaxElevationTime;
                var tuple = FindMaxElevation(satellite, maxElTime - deltaTime, maxElTime, maxElTime + deltaTime, resolution);

                var maxEl = tuple.Item1;
                var nextMaxElTime = tuple.Item2;
                maxElTime = nextMaxElTime;
                obs[0] = new SatelliteVisibilityPeriod(satellite, tu.CrossingPointTime, first.End, maxEl, maxElTime, first.ReferencePosition);
            }

            return obs;

        }

        public Topocentric Observe(Satellite satellite, Julian time)
        {
            var posEci = satellite.PositionEci(time);
            return Location.Observe(posEci, time);
        }

        public List<Topocentric> Propagate(Satellite satellite, Julian start, Julian end, TimeSpan deltaTime)
        {
            if ((end - start).TotalSeconds < 0)
                throw new ArgumentException("endTime must be greater than startTime", nameof(end));
            if (deltaTime.TotalSeconds <= 0)
                throw new ArgumentException("deltaTime must be positive", nameof(deltaTime));
            if (deltaTime <= TimeSpan.Zero)
                throw new ArgumentException("deltaTime must be greater than zero", nameof(deltaTime));

            List<Topocentric> observations = new List<Topocentric>();

            var t = start;
            var i = 1;
            while (t <= end)
            {
                var topo = GetTopo(satellite, t);
                observations.Add(topo);
                t = start + deltaTime * (i++);
            }

            return observations;
        }

        public bool IsVisible(Coordinate pos, Julian time, Angle minElevation = default)
        {
            var geo = pos.ToGeo();
            var angle = geo.GetVisibilityAngle();

            if (Location.AngleTo(geo) > angle)
                return false; // not visible if the angle to the ground station is greater than the visibility angle

            var topo = Location.Observe(pos, time);
            if (topo.Elevation < minElevation)
                return false; // not visible if the elevation is below the minimum
            return true;
        }


        // convenience function to get a topocentric observation for a given satellite and time
        private Topocentric GetTopo(Satellite satellite, Julian time)
        {
            var posEci = satellite.PositionEci(time);
            return Location.ToEci(time).Observe(posEci, time);
        }

        // finds the next crossing point in time when the observer's elevation changes from below minElevation to above.
        // if the observer's elevation at the start time is above or equal to minElevation, start is returned.
        private Julian? FindNextBelowToAboveCrossingPoint(Satellite satellite, Julian start, Julian end, TimeSpan deltaTime, Angle minElevation, int resolution)
        {
            var eciLocation = Location.ToEci(start);
            var posEci = satellite.PositionEci(start);

            var t = start - deltaTime;
            Julian prev;
            Angle el;

            do
            {
                prev = t;
                var next = t + deltaTime;
                t = next <= end ? next : end; // clamp t to end
                el = GetTopo(satellite, t).Elevation;
            } while (el < minElevation && t < end);

            if (prev == start)
            {
                return t;
            }

            if (el < minElevation)
            {
                return null;
            } // if we haven't found a crossing point

            // sort out tStart and tEnd
            Julian tStart, tEnd;
            if (prev < t)
            {
                tStart = prev;
                tEnd = t;
            }
            else
            {
                tStart = t;
                tEnd = prev;
            }

            return FindCrossingTimeWithinInterval(satellite, tStart, tEnd, minElevation, resolution);
        }

        // a POD structure that contains time of crossing point, max elevation, and time of max elevation
        private struct CrossingPointInfo
        {
            public CrossingPointInfo(Julian crossingPointTime, Julian maxElevationTime, Angle maxElevation)
            {
                CrossingPointTime = crossingPointTime;
                MaxElevationTime = maxElevationTime;
                MaxElevation = maxElevation;
            }

            public Julian CrossingPointTime { get; }
            public Julian MaxElevationTime { get; }
            public Angle MaxElevation { get; }
        }

        // finds the next crossing point in time when the observer's elevation changes from above minElevation to below.
        // if the observer's elevation at time start is below minElevation, the start time is returned.
        // note that deltaTime may be negative, i.e. this function can walk backwards in time as well as forwards.
        private CrossingPointInfo FindNextAboveToBelowCrossingPoint(Satellite satellite, Julian start, TimeSpan deltaTime, Angle minElevation, int resolution, Julian? end = null)
        {
            var eciLocation = Location.ToEci(start);
            var posEci = satellite.PositionEci(start);

            var t = start - deltaTime;
            Julian prev;
            var maxEl = Angle.Zero;
            var maxElTime = Julian.MinValue;
            Angle el;

            // we write two loops to make the check condition a little easier to read (and slightly more efficient)
            if (end is not null) // if an definite end time is specified
            {
                do
                {
                    prev = t;
                    t += deltaTime;
                    el = GetTopo(satellite, t).Elevation;
                    if (el > maxEl)
                    {
                        maxEl = el;
                        maxElTime = t;
                    }
                } while (el >= minElevation && t <= end);
            }
            else // if no definite end time is specified
            {
                do
                {
                    prev = t;
                    t += deltaTime;
                    el = GetTopo(satellite, t).Elevation;
                    if (el > maxEl)
                    {
                        maxEl = el;
                        maxElTime = t;
                    }
                } while (el >= minElevation);
            }

            if (t == start)
            {
                return new CrossingPointInfo(t, maxElTime, maxEl);
            } // bail out early if t==start

            Julian tStart, tEnd;
            // sort out tStart and tEnd
            if (prev < t)
            {
                tStart = prev;
                tEnd = t;
            }
            else
            {
                tStart = t;
                tEnd = prev;
            }

            t = FindCrossingTimeWithinInterval(satellite, tStart, tEnd, minElevation, resolution);
            return new CrossingPointInfo(t, maxElTime, maxEl);
        }


        // given a interval of time [start, end] with an crossing point within, determine the crossing point time 
        // it is assumed that the crossing point exists and is singular.
        private Julian FindCrossingTimeWithinInterval(Satellite satellite, Julian start, Julian end, Angle minElevation, int resolution)
        {
            if (start == end)
            {
                throw new ArgumentException("start and end cannot be equal", "start");
            }

            var startEl = GetTopo(satellite, start).Elevation;
            var endEl = GetTopo(satellite, end).Elevation;
            var isAscending = startEl < endEl;

            var tBelow = start;
            var tAbove = end;
            if (!isAscending)
            {
                tBelow = end;
                tAbove = start;
            }

            var minTicks = (long)(1e7 / Math.Pow(10, resolution)); // convert resolution (num decimals) to minimum ticks

            long dt;
            Julian t;

            // continually halve the interval until the size of the interval is less than minTicks
            do
            {
                dt = (tAbove - tBelow).Ticks / 2;
                t = Julian.AddTicks(tBelow, dt);
                var el = GetTopo(satellite, t).Elevation;
                if (el < minElevation)
                {
                    tBelow = t;
                }
                else
                {
                    tAbove = t;
                }
            } while (Math.Abs(dt) > minTicks);

            return t.Round(TimeSpan.FromTicks(minTicks)); // remove the trailing decimals
        }

        /*
        private Julian FindCrossingTimeWithinInterval(
			Satellite satellite,
			Julian tStart,
			Julian tEnd,
			Angle threshold,
			int resolution = 6)
        {
			int maxIterations = 100;
            double tol = 10e-5;

            double jdStart = tStart.Date;
            double jdEnd = tEnd.Date;

            Func<double, double> f = jd =>
            {
                var el = GetTopo(satellite, new Julian(jd)).Elevation.Degrees;
                return el - threshold.Degrees;
            };

            double a = jdStart;
            double b = jdEnd;
            double fa = f(a);
            double fb = f(b);

            if (fa * fb > 0)
                throw new InvalidOperationException("Elevation threshold is not bracketed in the interval.");

            for (int i = 0; i < maxIterations; i++)
            {
                double m = 0.5 * (a + b);
                double fm = f(m);

                if (Math.Abs(fm) < tol)
                {
                    return new Julian(m);
                }

                if (fa * fm < 0)
                {
                    b = m;
                    fb = fm;
                }
                else
                {
                    a = m;
                    fa = fm;
                }
            }

			throw new System.Exception("Method did not converge.");
        }*/



        // finds the max elevation and time for max elevation, to a given temporal resolution
        private Tuple<Angle, Julian> FindMaxElevation(Satellite satellite, Julian before, Julian peakTime, Julian after, int resolution)
        {
            var minTicks = (long)(1e7 / Math.Pow(10, resolution)); // convert resolution (num decimals) to minimum ticks

            do
            {
                var elBefore = GetTopo(satellite, before).Elevation;
                var elAfter = GetTopo(satellite, after).Elevation;
                var elPeakTime = GetTopo(satellite, peakTime).Elevation;

                var t1 = before + TimeSpan.FromTicks((peakTime - before).Ticks / 2);
                var t2 = peakTime + TimeSpan.FromTicks((after - peakTime).Ticks / 2);

                var elT1 = GetTopo(satellite, t1).Elevation;
                var elT2 = GetTopo(satellite, t2).Elevation;

                // temporal ordering is: before, t1, peakTime, t2, after

                // find max of {elT1, elPeakTime, elT2} and choose new (before, peakTime, after) appropriately
                if (elT1 > elPeakTime && elT1 > elT2)
                {
                    after = peakTime;
                    peakTime = t1;
                }
                else if (elPeakTime > elT1 && elPeakTime > elT2)
                {
                    before = t1;
                    after = t2;
                }
                else // elT2 is max
                {
                    before = peakTime;
                    peakTime = t2;
                }
            } while ((after - before).Ticks > minTicks);

            return Tuple.Create(GetTopo(satellite, peakTime).Elevation, peakTime.Round(TimeSpan.FromTicks(minTicks))); // remove the trailing decimals);
        }

        internal static DateTime DateTimeRound(DateTime date, TimeSpan span)
        {
            var ticks = (date.Ticks + span.Ticks / 2 + 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks, date.Kind);
        }
    }
}
