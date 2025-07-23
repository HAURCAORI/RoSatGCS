using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Utils.Satellites.Core
{
    public struct Angle
    {
        public double Radians { get; }
        public double Degrees => Globals.ToDegrees(Radians);

        public static readonly Angle Zero = new Angle(0);

        private Angle(double angle)
        {
            Radians = angle;
        }
        public static Angle FromRadians(double radians) => new Angle(radians);

        public static Angle FromDegrees(double degrees) => new Angle(Globals.ToRadians(degrees));



        public override string ToString()
        {
            return $"{Degrees}°";
        }

        public string ToDegreesMinutesSeconds()
        {
            var dd = Degrees;
            var d = Math.Floor(dd);
            var m = Math.Floor(dd % 60);
            var s = (dd - d - m / 60) * 60 * 60;
            return $"{d}°{m:00}'{s:F2}\"";
        }


        public override bool Equals(object? obj)
        {
            return obj is Angle angle && Radians.Equals(angle.Radians);
        }

        public static bool operator ==(Angle angle1, Angle angle2)
        {
            return EqualityComparer<Angle>.Default.Equals(angle1, angle2);
        }

        public static bool operator !=(Angle angle1, Angle angle2)
        {
            return !(angle1 == angle2);
        }

        public static bool operator >(Angle angle1, Angle angle2)
        {
            return angle1.Radians > angle2.Radians;
        }

        public static bool operator <(Angle angle1, Angle angle2)
        {
            return angle1.Radians < angle2.Radians;
        }

        public static bool operator >=(Angle angle1, Angle angle2)
        {
            return angle1.Radians >= angle2.Radians;
        }

        public static bool operator <=(Angle angle1, Angle angle2)
        {
            return angle1.Radians <= angle2.Radians;
        }

        public static Angle operator +(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Radians + angle2.Radians);
        }

        public static Angle operator -(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Radians - angle2.Radians);
        }

        public static implicit operator Angle(double d)
        {
            return Angle.FromDegrees(d);
        }

        public override int GetHashCode()
        {
            return 1530437289 + Radians.GetHashCode();
        }


    }
}
