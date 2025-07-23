using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RoSatGCS.Utils.Satellites.Core
{
    public class EciCoordinate : Coordinate
    {
        public Julian Date { get; protected set; }

        public Vector Position { get; protected set; }
        public Vector Velocity { get; protected set; }

        public EciCoordinate(Julian date) : this(new Vector(), new Vector(), date) { }

        public EciCoordinate(Vector position, Julian date) : this(position, new Vector(), date) { }
        public EciCoordinate(Vector position, Vector velocity, Julian date)
        {
            Position = position;
            Velocity = velocity;
            Date = date;
        }

        public EciCoordinate(Coordinate coordinate, Julian date)
        {
            var eci = coordinate.ToEci(date);
            Position = eci.Position;
            Velocity = eci.Velocity;
            Date = date;
        }

        public override EciCoordinate ToEci(Julian dt)
        {
            return Math.Abs((dt - Date).Milliseconds) < 1 ? this : ToGeo().ToEci(dt);
        }

        public override GeoCoordinate ToGeo()
        {
            double theta = (Globals.AcTan(Position.Y, Position.X) - Date.ToGmst()) % Globals.TwoPi;

            theta = theta % Globals.TwoPi;

            if (theta < 0.0)
            {
                // "wrap" negative modulo
                theta += Globals.TwoPi;
            }

            double r = Math.Sqrt(Globals.Sqr(Position.X) + Globals.Sqr(Position.Y));
            double e2 = Globals.F * (2.0 - Globals.F);
            double lat = Globals.AcTan(Position.Z, r);

            const double DELTA = 1.0e-07;
            double phi;
            double c;

            do
            {
                phi = lat;
                c = 1.0 / Math.Sqrt(1.0 - e2 * Globals.Sqr(Math.Sin(phi)));
                lat = Globals.AcTan(Position.Z + Globals.Xkmper * c * e2 * Math.Sin(phi), r);
            }
            while (Math.Abs(lat - phi) > DELTA);

            Angle latitude = Angle.FromRadians(lat);
            Angle longitude = Angle.FromRadians(theta);
            double altitude = (r / Math.Cos(lat)) - Globals.Xkmper * c;

            return new GeoCoordinate(latitude, longitude, altitude);
        }


        /// <summary>
        /// Scale the position vector by a factor.
        /// </summary>
        public void ScalePosVector(double factor)
        {
            Position.Scale(factor);
        }

        /// <summary>
        /// Scale the velocity vector by a factor.
        /// </summary>
        public void ScaleVelVector(double factor)
        {
            Velocity.Scale(factor);
        }

        public override bool Equals(object? obj)
        {
            return obj is EciCoordinate eci && Equals(eci);
        }

        public bool Equals(EciCoordinate other)
        {
            return base.Equals(other) && Date.Equals(other.Date) && Equals(Position, other.Position) &&
                   Equals(Velocity, other.Velocity);
        }

        public static bool operator ==(EciCoordinate left, EciCoordinate right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(EciCoordinate left, EciCoordinate right)
        {
            return !Equals(left, right);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Date, Position, Velocity);
        }
    }
}
