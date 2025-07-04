//
// Globals.cs
//
// Copyright (c) 2003-2021 Michael F. Henry
// Version 09/2021
//
using System;
using System.Runtime.Serialization;

namespace RoSatGCS.Utils.Satellites.Core
{
    /// <summary>
    /// Numerical constants.
    /// </summary>
    public static class Globals
    {
        #region Constants
        public const double LightSpeed = 299792.458; // Speed of Light km/sec

        public const double Pi = 3.141592653589793;
        public const double TwoPi = 2.0 * Pi;
        public const double RadsPerDegree = Pi / 180.0;
        public const double DegreesPerRad = 180.0 / Pi;

        public const double Gm = 398601.2;   // Earth gravitational constant, km^3/sec^2
        public const double GeoSyncAlt = 42241.892;  // km
        public const double EarthDiam = 12800.0;    // km
        public const double EarthRadius = 6378.135;   // km (WGS '72)
        public const double DaySidereal = (23 * 3600) + (56 * 60) + 4.09;  // sec
        public const double DaySolar = (24 * 3600);   // sec

        public const double Ae = 1.0;
        public const double Au = 149597870.0;  // Astronomical unit (km) (IAU 76)
        public const double Sr = 696000.0;     // Solar radius (km)      (IAU 76)
        public const double Xkmper = 6378.135;     // Earth equatorial radius - kilometers (WGS '72)
        public const double F = 1.0 / 298.26; // Earth flattening (WGS '72)
        public const double Ge = 398600.8;     // Earth gravitational constant (WGS '72)
        public const double J2 = 1.0826158E-3; // J2 harmonic (WGS '72)
        public const double J3 = -2.53881E-6;  // J3 harmonic (WGS '72)
        public const double J4 = -1.65597E-6;  // J4 harmonic (WGS '72)
        public const double Ck2 = J2 / 2.0;
        public const double Ck4 = -3.0 * J4 / 8.0;
        public const double Xj3 = J3;
        public const double Qo = Globals.Ae + 120.0 / Globals.Xkmper;
        public const double S = Globals.Ae + 78.0 / Globals.Xkmper;
        public const double HoursPerDay = 24.0;          // Hours per day   (solar)
        public const double MinPerDay = 1440.0;        // Minutes per day (solar)
        public const double SecPerDay = 86400.0;       // Seconds per day (solar)
        public const double OmegaE = 1.00273790934; // Earth rotation per sidereal day
        public static double Xke = Math.Sqrt(3600.0 * Ge /
                                             (Globals.Xkmper * Globals.Xkmper * Globals.Xkmper)); // sqrt(ge) ER^3/min^2
        public static double Qoms2t = Math.Pow((Qo - Globals.S), 4); //(QO - S)^4 ER^4

        #endregion

        #region Utility

        // ///////////////////////////////////////////////////////////////////////////
        public static double Sqr(double x)
        {
            return (x * x);
        }

        // ///////////////////////////////////////////////////////////////////////////
        public static double Fmod2p(double arg)
        {
            double modu = (arg % TwoPi);

            if (modu < 0.0)
            {
                modu += TwoPi;
            }

            return modu;
        }

        // ///////////////////////////////////////////////////////////////////////////
        // Globals.AcTan()
        // ArcTangent of sin(x) / cos(x). The advantage of this function over arctan()
        // is that it returns the correct quadrant of the angle.
        public static double AcTan(double sinx, double cosx)
        {
            double ret;

            if (cosx == 0.0)
            {
                if (sinx > 0.0)
                {
                    ret = Pi / 2.0;
                }
                else
                {
                    ret = 3.0 * Pi / 2.0;
                }
            }
            else
            {
                if (cosx > 0.0)
                {
                    ret = Math.Atan(sinx / cosx);
                }
                else
                {
                    ret = Pi + Math.Atan(sinx / cosx);
                }
            }

            return ret;
        }

        public static double ToDegrees(this double radians) { return radians * DegreesPerRad; }
        public static double ToRadians(this double degrees) { return degrees * RadsPerDegree; }

        public static double Mod(double x, double y)
        {
            if (y == 0)
                return x;

            return x - y * Math.Floor(x / y);
        }


        public static double WrapNegPosPi(double a)
        {
            return Mod(a + Math.PI, TwoPi) - Math.PI;
        }

        public static double WrapTwoPi(double a)
        {
            return Mod(a, TwoPi);
        }
        #endregion
    }
}
