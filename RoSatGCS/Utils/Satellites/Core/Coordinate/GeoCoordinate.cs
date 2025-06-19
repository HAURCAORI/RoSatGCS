using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RoSatGCS.Utils.Satellites.Core
{
    public class GeoCoordinate : Coordinate
    {
        public Angle Latitude { get; }
        public Angle Longitude { get; }
        public double Altitude { get; }

        public GeoCoordinate() { }

        public GeoCoordinate(Angle lat, Angle lon, double altitude)
        {
            Latitude  = lat;
            Longitude = lon;
            Altitude  = altitude;
        }

        public  GeoCoordinate(Coordinate coordinate)
        {
            var geo = coordinate.ToGeo();
            Latitude  = geo.Latitude;
            Longitude = geo.Longitude;
            Altitude  = geo.Altitude;
        }


        public override EciCoordinate ToEci(Julian date)
        {
            double lat = Latitude.Radians;
            double lon = Longitude.Radians;
            double alt = Altitude;

            // Calculate Local Mean Sidereal Time (theta)
            double theta = date.ToLmst(lon);
            double c = 1.0 / Math.Sqrt(1.0 + Globals.F * (Globals.F - 2.0) *
                             Globals.Sqr(Math.Sin(lat)));
            double s = Globals.Sqr(1.0 - Globals.F) * c;
            double achcp = (Globals.Xkmper * c + alt) * Math.Cos(lat);

            Vector position = new Vector();

            position.X = achcp * Math.Cos(theta);             // km
            position.Y = achcp * Math.Sin(theta);             // km
            position.Z = (Globals.Xkmper * s + alt) * Math.Sin(lat);   // km
            position.W = Math.Sqrt(Globals.Sqr(position.X) +
                                   Globals.Sqr(position.Y) +
                                   Globals.Sqr(position.Z));  // range, km

            Vector velocity = new Vector();
            double mfactor = Globals.TwoPi * (Globals.OmegaE / Globals.SecPerDay);

            velocity.X = -mfactor * position.Y;               // km / sec
            velocity.Y = mfactor * position.X;               // km / sec
            velocity.Z = 0.0;                                 // km / sec
            velocity.W = Math.Sqrt(Globals.Sqr(velocity.X) +  // range rate km/sec^2
                                   Globals.Sqr(velocity.Y));

            return new EciCoordinate(position, velocity, date);
        }

        public override GeoCoordinate ToGeo()
        {
            return this;
        }

        public override bool Equals(object? obj)
        {
            return obj is GeoCoordinate geodetic && Equals(geodetic);
        }

        public bool Equals(GeoCoordinate other)
        {
            return Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude) && Altitude.Equals(other.Altitude);
        }

        public static bool operator ==(GeoCoordinate left, GeoCoordinate right)
        {
            return Equals(left, right);
        }
        public static bool operator !=(GeoCoordinate left, GeoCoordinate right)
        {
            return !Equals(left, right);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Latitude, Longitude, Altitude);
        }
    }
}
