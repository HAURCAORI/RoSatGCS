using System;
using System.Collections.Generic;

namespace RoSatGCS.Utils.Satellites.Core
{
    /// <summary>
    /// Abstract base class representing a spatial coordinate.
    /// </summary>
    public abstract class Coordinate
    {
        /// <summary>Converts this coordinate to ECI at the given Julian time.</summary>
        public abstract EciCoordinate ToEci(Julian dt);

        /// <summary>Converts this coordinate to geodetic format.</summary>
        public abstract GeoCoordinate ToGeo();

        /// <summary>Computes the Earth central angle beyond which this coordinate cannot see.</summary>
        public Angle GetVisibilityAngle()
        {
            var geo = ToGeo();
            return Angle.FromRadians(Math.Acos(Globals.EarthRadius / (Globals.EarthRadius + geo.Altitude)));
        }

        /// <summary>Computes the surface radius visible from this coordinate.</summary>
        public double GetVisibilityRadius()
        {
            return GetVisibilityAngle().Radians * Globals.EarthRadius;
        }

        /// <summary>
		///     Gets a list of geodetic coordinates which define the bounds of the visibility footprint at a specific time
		/// </summary>
		/// <param name="numPoints">The number of points in the resulting circle</param>
		/// <returns>A list of geodetic coordinates for the specified time</returns>
        public List<GeoCoordinate> GetVisibilityBoundary(int numPoints)
        {
            var center = ToGeo();
            var coords = new List<GeoCoordinate>();

            var lat = center.Latitude;
            var lon = center.Longitude;
            var d = center.GetVisibilityAngle().Radians;

            for (var i = 0; i < numPoints; i++)
            {
                var perc = i / (float)numPoints * 2 * Math.PI;

                var latRadians = Math.Asin(Math.Sin(lat.Radians) * Math.Cos(d) +
                                           Math.Cos(lat.Radians) * Math.Sin(d) * Math.Cos(perc));
                var lngRadians = lon.Radians +
                                 Math.Atan2(Math.Sin(perc) * Math.Sin(d) * Math.Cos(lat.Radians),
                                     Math.Cos(d) - Math.Sin(lat.Radians) * Math.Sin(latRadians));

                lngRadians = Globals.WrapNegPosPi(lngRadians);

                coords.Add(new GeoCoordinate(Angle.FromRadians(latRadians), Angle.FromRadians(lngRadians), 10));
            }

            return coords;
        }

        /// <summary>
		///     Calculates the look angles between this coordinate and target
		/// </summary>
		/// <param name="time">The time of observation</param>
		/// <param name="to">The coordinate to observe</param>
		/// <returns>The topocentric angles between this coordinate and another</returns>
		public Topocentric Observe(Coordinate to, Julian? time = null)
        {
            var t = time ?? new Julian();

            var geo = ToGeo();
            var eci = to.ToEci(t);
            var self = ToEci(t);

            var rangeRate = eci.Velocity - self.Velocity;
            var range = eci.Position - self.Position;

            var theta = eci.Date.ToLmst(geo.Longitude.Radians);

            var sinLat = Math.Sin(geo.Latitude.Radians);
            var cosLat = Math.Cos(geo.Latitude.Radians);
            var sinTheta = Math.Sin(theta);
            var cosTheta = Math.Cos(theta);

            var topS = sinLat * cosTheta * range.X
                + sinLat * sinTheta * range.Y - cosLat * range.Z;
            var topE = -sinTheta * range.X
                       + cosTheta * range.Y;
            var topZ = cosLat * cosTheta * range.X
                       + cosLat * sinTheta * range.Y + sinLat * range.Z;
            var az = Math.Atan(-topE / topS);

            if (topS > 0.0)
                az += Math.PI;

            if (az < 0.0)
                az += 2.0 * Math.PI;

            var el = Math.Asin(topZ / range.Magnitude());
            var rate = range.Dot(rangeRate) / range.Magnitude();

            return new Topocentric(Angle.FromRadians(az), Angle.FromRadians(el), range.Magnitude(), rate, t, this);
        }

        /// <summary>
		///     Calculates the Great Circle distance (km) to another coordinate
		/// </summary>
		/// <param name="to">The coordinate to measure against</param>
		/// <returns>The distance between the coordinates, in kilometers</returns>
		public double DistanceTo(Coordinate to)
        {
            return AngleTo(to).Radians * Globals.EarthRadius;
        }

        /// <summary>
        ///     Calculates the Great Circle distance as an angle to another geodetic coordinate, ignoring altitude
        /// </summary>
        /// <param name="to">The coordinate to measure against</param>
        /// <returns>The distance between the coordinates as an angle across Earth's surface</returns>
        public Angle AngleTo(Coordinate to)
        {
            var geo = ToGeo();
            var toGeo = to.ToGeo();
            var dist = Math.Sin(geo.Latitude.Radians) * Math.Sin(toGeo.Latitude.Radians) +
                       Math.Cos(geo.Latitude.Radians) * Math.Cos(toGeo.Latitude.Radians) *
                       Math.Cos(geo.Longitude.Radians - toGeo.Longitude.Radians);
            dist = Math.Acos(dist);

            return Angle.FromRadians(dist);
        }

        /// <summary>
		///     Returns true if there is line-of-sight between this coordinate and the supplied one by checking if this coordinate
		///     is within the footprint of the other
		/// </summary>
		/// <param name="other">The coordinate to check against</param>
		/// <returns>True if there is line-of-sight between this coordinate and the supplied one</returns>
		public bool CanSee(Coordinate other)
        {
            return AngleTo(other) < other.GetVisibilityAngle();
        }

        #region Equals and Operators
        public override bool Equals(object? obj)
        {
            return obj is Coordinate coord && Equals(coord);
        }

        protected bool Equals(Coordinate other)
        {
            return ToGeo().Equals(other.ToGeo());
        }

        public static bool operator ==(Coordinate left, Coordinate right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(Coordinate left, Coordinate right)
        {
            return !Equals(left, right);
        }

        public override int GetHashCode()
        {
            return ToGeo().GetHashCode();
        }

        #endregion
    }
}
