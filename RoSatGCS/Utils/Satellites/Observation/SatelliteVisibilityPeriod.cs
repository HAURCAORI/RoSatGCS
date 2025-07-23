
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoSatGCS.Utils.Satellites.Core;
using RoSatGCS.Utils.Satellites.Observation;

namespace RoSatGCS.Utils.Satellites.Observation
{
    public class SatelliteVisibilityPeriod
    {
        public Satellite Satellite { get; }
        public Julian Start { get; }
        public Julian End { get; }
        public Angle MaxElevation { get; }
        public Julian MaxElevationTime { get; }
        public Coordinate? ReferencePosition { get; }

        public SatelliteVisibilityPeriod(Satellite satellite, Julian start, Julian end, Angle maxElevation, Julian maxElevationTime, Coordinate? referencePosition = null)
        {
            this.Satellite = satellite;
            this.Start = start;
            this.End = end;
            this.MaxElevation = maxElevation;
            this.MaxElevationTime = maxElevationTime;
            this.ReferencePosition = referencePosition;
        }

        protected bool Equals(SatelliteVisibilityPeriod other)
        {
            return Satellite.Equals(other.Satellite) &&
                   Start.Equals(other.Start) &&
                   End.Equals(other.End) &&
                   MaxElevation.Equals(other.MaxElevation) &&
                   MaxElevationTime.Equals(other.MaxElevationTime) &&
                   Equals(ReferencePosition, other.ReferencePosition);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SatelliteVisibilityPeriod)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Satellite.GetHashCode();
                hashCode = (hashCode * 397) ^ Start.GetHashCode();
                hashCode = (hashCode * 397) ^ End.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxElevation.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxElevationTime.GetHashCode();
                hashCode = (hashCode * 397) ^ (ReferencePosition?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public static bool operator ==(SatelliteVisibilityPeriod left, SatelliteVisibilityPeriod right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SatelliteVisibilityPeriod left, SatelliteVisibilityPeriod right)
        {
            return !Equals(left, right);
        }
    }
}
