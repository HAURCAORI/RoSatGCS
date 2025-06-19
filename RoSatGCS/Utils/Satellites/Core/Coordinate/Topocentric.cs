using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RoSatGCS.Utils.Satellites.Core
{
    public class Topoentric
    {
        public Angle Azimuth { get; protected set; }
        public Angle Elevation { get; protected set; }

        public double Range { get; }
        public double RangeRate { get; }

        public Julian Date { get; }
        public Coordinate? ReferencePosition { get; }

        public RelativeDirection Direction => GetRelativeDirection();

        public double SignalDelay => GetSignalDelay();

        /// <summary>
        ///     Stores a topocentric location (azimuth, elevation, range and range rate).
        /// </summary>
        /// <param name="azimuth">Azimuth relative to the observer</param>
        /// <param name="elevation">Elevation relative to the observer</param>
        /// <param name="range">Range relative to the observer, in kilometers</param>
        /// <param name="rangeRate">Range rate relative to the observer, in kilometers/second</param>
        /// <param name="referencePosition">The position from which the satellite was observed to generate this observation</param>
        public Topoentric(Angle azimuth, Angle elevation, double range, double rangeRate, Julian date, Coordinate? referencePosition = null)
        {
            this.Azimuth = azimuth;
            this.Elevation = elevation;
            this.Range = range;
            this.RangeRate = rangeRate;
            this.Date = date;
            this.ReferencePosition = referencePosition;
        }

        public Topoentric() : this(Angle.Zero, Angle.Zero, 0, 0, new Julian()) { }

        public Topoentric(Topoentric topo)
        {
            Azimuth = topo.Azimuth;
            Elevation = topo.Elevation;
            Range = topo.Range;
            RangeRate = topo.RangeRate;
            Date = topo.Date;
            ReferencePosition = topo.ReferencePosition;
        }

        private double GetSignalDelay()
        {
            return Globals.LightSpeed / Range;
        }

        private RelativeDirection GetRelativeDirection()
        {
            if (Math.Abs(RangeRate) < double.Epsilon) return RelativeDirection.Fixed;
            return RangeRate < 0 ? RelativeDirection.Approaching : RelativeDirection.Receding;
        }

        /// <summary>
		///     Predicts the doppler shift of the satellite relative to the observer, in Hz
		/// </summary>
		/// <param name="inputFrequency">The base RX/TX frequency, in Hz</param>
		/// <returns>The doppler shift of the satellite</returns>
		public double GetDopplerShift(double inputFrequency)
        {
            return - RangeRate / Globals.LightSpeed * inputFrequency;
        }

        protected bool Equals(Topoentric other)
        {
            return Azimuth.Equals(other.Azimuth) && Elevation.Equals(other.Elevation) && Range.Equals(other.Range) && RangeRate.Equals(other.RangeRate) &&
                   Equals(ReferencePosition, other.ReferencePosition);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Topoentric)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Azimuth.GetHashCode();
                hashCode = (hashCode * 397) ^ Elevation.GetHashCode();
                hashCode = (hashCode * 397) ^ Range.GetHashCode();
                hashCode = (hashCode * 397) ^ RangeRate.GetHashCode();
                hashCode = (hashCode * 397) ^ (ReferencePosition is not null ? ReferencePosition.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(Topoentric left, Topoentric right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Topoentric left, Topoentric right)
        {
            return !Equals(left, right);
        }
    }
}
