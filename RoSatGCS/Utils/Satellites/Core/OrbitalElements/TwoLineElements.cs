//
// TwoLineElements.cs
//
// Copyright (c) 2021-2022 Michael F. Henry
// Version 07/2022
//
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;

// ////////////////////////////////////////////////////////////////////////
//
// Two-Line Element Data format
//
// [Reference: Dr. T.S. Kelso / www.celestrak.com]
//
// Two-line element data consists of three lines in the following format:
//
// AAAAAAAAAAAAAAAAAAAAAAAA
// 1 NNNNNU NNNNNAAA NNNNN.NNNNNNNN +.NNNNNNNN +NNNNN-N +NNNNN-N N NNNNN
// 2 NNNNN NNN.NNNN NNN.NNNN NNNNNNN NNN.NNNN NNN.NNNN NN.NNNNNNNNNNNNNN
//  
// Line 0 is a twenty-four-character name.
// 
// Lines 1 and 2 are the standard Two-Line Orbital Element Set Format 
// used by NORAD and NASA. The format description is:
//      
//     Line 1
//     Column    Description
//     01-01     Line Number of Element Data
//     03-07     Satellite Number
//     10-11     International Designator (Last two digits of launch year)
//     12-14     International Designator (Launch number of the year)
//     15-17     International Designator (Piece of launch)
//     19-20     Epoch Year (Last two digits of year)
//     21-32     Epoch (Julian Day and fractional portion of the day)
//     34-43     First Time Derivative of the Mean Motion
//               or Ballistic Coefficient (Depending on ephemeris type)
//     45-52     Second Time Derivative of Mean Motion (decimal point assumed;
//               blank if N/A)
//     54-61     BSTAR drag term if GP4 general perturbation theory was used.
//               Otherwise, radiation pressure coefficient.  (Decimal point assumed)
//     63-63     Ephemeris type
//     65-68     Element number
//     69-69     Check Sum (Modulo 10)
//               (Letters, blanks, periods, plus signs = 0; minus signs = 1)
//     Line 2
//     Column    Description
//     01-01     Line Number of Element Data
//     03-07     Satellite Number
//     09-16     Inclination [Degrees]
//     18-25     Right Ascension of the Ascending Node [Degrees]
//     27-33     Eccentricity (decimal point assumed)
//     35-42     Argument of Perigee [Degrees]
//     44-51     Mean Anomaly [Degrees]
//     53-63     Mean Motion [Revs per day]
//     64-68     Revolution number at epoch [Revs]
//     69-69     Check Sum (Modulo 10)
//        
//     All other columns are blank or fixed.
//          
// Example:
//      
// ISS(ZARYA)
// 1 25544U 98067A   16362.88986010  .00002353  00000-0  43073-4 0  9992
// 2 25544  51.6423 172.2304 0006777  22.6708 127.4688 15.53951055 35119
//
namespace RoSatGCS.Utils.Satellites.Core
{
    /// <summary>
    /// This class encapsulates a single set of NORAD two-line orbital elements.
    /// </summary>
    public sealed class TwoLineElements : OrbitalElements
    {
        #region Properties
        public char Class { get; private set; } = ' ';

        #endregion

        #region Column Offsets

        // Note: Column offsets are zero-based.

        // Line 1
        private const int TLE1_COL_SATNUM = 2; private const int TLE1_LEN_SATNUM = 5;
        private const int TLE1_COL_CLASS = 7; private const int TLE1_LEN_CLASS = 1;
        private const int TLE1_COL_INTLDESC_A = 9; private const int TLE1_LEN_INTLDESC_A = 2;
        private const int TLE1_LEN_INTLDESC_B = 3; private const int TLE1_LEN_INTLDESC_C = 3;
        private const int TLE1_COL_EPOCH_A = 18; private const int TLE1_LEN_EPOCH_A = 2;
        private const int TLE1_COL_EPOCH_B = 20; private const int TLE1_LEN_EPOCH_B = 12;
        private const int TLE1_COL_MEANMOTIONDT = 33; private const int TLE1_LEN_MEANMOTIONDT = 10;
        private const int TLE1_COL_MEANMOTIONDT2 = 44; private const int TLE1_LEN_MEANMOTIONDT2 = 8;
        private const int TLE1_COL_BSTAR = 53; private const int TLE1_LEN_BSTAR = 8;
        private const int TLE1_COL_EPHEMERIS = 62; private const int TLE1_LEN_EPHEMERIS = 1;
        private const int TLE1_COL_ELNUM = 64; private const int TLE1_LEN_ELNUM = 4;
        private const int TLE1_COL_CHECKSUM = 68; private const int TLE1_LEN_CHECKSUM = 1;

        // Line 2
        private const int TLE2_COL_SATNUM = 2; private const int TLE2_LEN_SATNUM = 5;
        private const int TLE2_COL_INCLINATION = 8; private const int TLE2_LEN_INCLINATION = 8;
        private const int TLE2_COL_RAASCENDNODE = 17; private const int TLE2_LEN_RAASCENDNODE = 8;
        private const int TLE2_COL_ECCENTRICITY = 26; private const int TLE2_LEN_ECCENTRICITY = 7;
        private const int TLE2_COL_ARGPERIGEE = 34; private const int TLE2_LEN_ARGPERIGEE = 8;
        private const int TLE2_COL_MEANANOMALY = 43; private const int TLE2_LEN_MEANANOMALY = 8;
        private const int TLE2_COL_MEANMOTION = 52; private const int TLE2_LEN_MEANMOTION = 11;
        private const int TLE2_COL_REVATEPOCH = 63; private const int TLE2_LEN_REVATEPOCH = 5;

        #endregion

        private static double Parse(string str) { return double.Parse(str, CultureInfo.InvariantCulture); }
        private static int ParseInt(string str) { return int.Parse(str, CultureInfo.InvariantCulture); }

        public static bool ChecksumValidatation(string line)
        {
            if (line.Length != 69) { return false; }
            // Checksum is the sum of all digits in the line, mod 10
            int sum = 0;
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (char.IsDigit(line[i]))
                {
                    sum += int.Parse(line[i].ToString(), CultureInfo.InvariantCulture);
                }
                else if (line[i] == '-')
                {
                    sum += 1;
                }
            }
            char ret = (char)((sum % 10) + '0');
            return ret == line.Last();
        }

        public static int ComputeChecksum(string line)
        {
            if (line.Length != 69 && line.Length != 68) { return ' '; }
            // Checksum is the sum of all digits in the line, mod 10
            int sum = 0;
            for (int i = 0; i < 68; i++)
            {
                if (char.IsDigit(line[i]))
                {
                    sum += int.Parse(line[i].ToString(), CultureInfo.InvariantCulture);
                }
                else if (line[i] == '-')
                {
                    sum += 1;
                }
            }
            return (sum % 10);
        }

        private static string AlignTLELine1(string line1)
        {
            var parts = line1.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 9)
                throw new FormatException("Line 1 does not contain enough fields.");

            string line = string.Format(CultureInfo.InvariantCulture,
                "1 {0,-6} {1,-8} {2,-14} {3,10} {4,8} {5,8} {6} {7,5}",
                parts[1],                      // NORAD ID
                parts[2].PadRight(8, ' '),     // Intl Designator
                parts[3].PadRight(14, ' '),    // Epoch
                parts[4].PadLeft(10),          // Mean motion dot
                parts[5].PadLeft(8),           // Mean motion dot dot
                parts[6].PadLeft(8),           // B*
                parts[7],                      // Ephemeris type
                parts[8].PadLeft(5)            // Element number
            );

            return line.PadRight(69);
        }

        private static string AlignTLELine2(string line2)
        {
            var parts = line2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 8)
                throw new FormatException("Line 2 does not contain enough fields.");

            string line = string.Format(CultureInfo.InvariantCulture,
                "2 {0,-5} {1,8} {2,8} {3,7} {4,8} {5,8} {6,17}",
                parts[1].PadLeft(5),              // NORAD ID
                parts[2].PadLeft(8),              // Inclination
                parts[3].PadLeft(8),              // RAAN
                parts[4].PadLeft(7, '0'),         // Eccentricity (no decimal)
                parts[5].PadLeft(8),              // Argument of perigee
                parts[6].PadLeft(8),              // Mean anomaly
                parts[7].PadLeft(17)              // Mean motion
            );

            return line.PadRight(69);
        }

        #region Construction

        /// <summary>
        /// Standard constructor.
        /// </summary>
        /// <param name="name">The satellite name.</param>
        /// <param name="line1">Line 1 of the orbital elements.</param>
        /// <param name="line2">Line 2 of the orbital elements.</param>
        public TwoLineElements(string name, string line1, string line2)
        {
            SatelliteName = name;

            line1 = AlignTLELine1(line1);
            line2 = AlignTLELine2(line2);

            // Check for valid TLE length
            if (line1.Length != 69 || line2.Length != 69)
            {
                throw new FormatException("Invalid TLE length.");
            }

            // Check for valid checksum
            if (!ChecksumValidatation(line1) || !ChecksumValidatation(line2))
            {
                throw new FormatException("Invalid TLE checksum.");
            }

            // Line 1 validation
            if (line1[0] != '1')
            {
                throw new FormatException("Invalid TLE line 1.");
            }

            // Column 3-7
            NoradIdStr = line1.Substring(TLE1_COL_SATNUM, TLE1_LEN_SATNUM);

            // Column 8
            string classStr = line1.Substring(TLE1_COL_CLASS, TLE1_LEN_CLASS);
            if (classStr != "U" && classStr != "C" && classStr != "S")
            {
                throw new FormatException("Invalid TLE line 1.");
            }
            Class = classStr[0];

            // Column 10-17
            IntlDesignatorStr = line1.Substring(TLE1_COL_INTLDESC_A,
                                           TLE1_LEN_INTLDESC_A +
                                           TLE1_LEN_INTLDESC_B +
                                           TLE1_LEN_INTLDESC_C).
                                           Replace(" ", string.Empty);

            // Column 19-20 & 21-32
            int epochYear = ParseInt(line1.Substring(TLE1_COL_EPOCH_A, TLE1_LEN_EPOCH_A));
            double epochDay = Parse(line1.Substring(TLE1_COL_EPOCH_B, TLE1_LEN_EPOCH_B));
            epochYear = (epochYear < 57) ? (epochYear + 2000) : (epochYear + 1900);
            Epoch = new Julian(epochYear, epochDay);

            // Column 34-43
            string dragStr = (line1[TLE1_COL_MEANMOTIONDT] == '-') ? "-0" : "0";
            dragStr += line1.Substring(TLE1_COL_MEANMOTIONDT + 1, TLE1_LEN_MEANMOTIONDT);
            MeanMotionDt = Parse(dragStr);

            // Column 45-52
            MeanMotionDt2 = Parse(ExpToDecimal(line1.Substring(TLE1_COL_MEANMOTIONDT2, TLE1_LEN_MEANMOTIONDT2)));

            // Column 54-61
            // Decimal point assumed; exponential notation
            BStar = Parse(ExpToDecimal(line1.Substring(TLE1_COL_BSTAR, TLE1_LEN_BSTAR)));

            // Column 63
            string ephemerisStr = line1.Substring(TLE1_COL_EPHEMERIS, TLE1_LEN_EPHEMERIS);
            // Ephemeris type is not used in this implementation

            // Column 65-68
            string setNumStr = line1.Substring(TLE1_COL_ELNUM, TLE1_LEN_ELNUM).TrimStart();
            if (string.IsNullOrEmpty(setNumStr)) { setNumStr = "0"; }
            SetNumber = ParseInt(setNumStr);

            // Column 69
            string checksumStr = line1.Substring(TLE1_COL_CHECKSUM, TLE1_LEN_CHECKSUM);

            // Line 2 validation
            if (line2[0] != '2')
            {
                throw new FormatException("Invalid TLE line 2.");
            }

            // Column 3-7
            string satNumStr = line2.Substring(TLE2_COL_SATNUM, TLE2_LEN_SATNUM);
            if (satNumStr != NoradIdStr)
            {
                throw new FormatException("Invalid TLE line 2.");
            }

            // Column 9-16
            InclinationDeg = Parse(line2.Substring(TLE2_COL_INCLINATION, TLE2_LEN_INCLINATION));

            // Column 18-25
            RAANodeDeg = Parse(line2.Substring(TLE2_COL_RAASCENDNODE, TLE2_LEN_RAASCENDNODE));

            // Column 27-33
            Eccentricity = Parse("0." + line2.Substring(TLE2_COL_ECCENTRICITY, TLE2_LEN_ECCENTRICITY));

            // Column 35-42
            ArgPerigeeDeg = Parse(line2.Substring(TLE2_COL_ARGPERIGEE, TLE2_LEN_ARGPERIGEE));

            // Column 44-51
            MeanAnomalyDeg = Parse(line2.Substring(TLE2_COL_MEANANOMALY, TLE2_LEN_MEANANOMALY));

            // Column 53-63
            MeanMotion = Parse(line2.Substring(TLE2_COL_MEANMOTION, TLE2_LEN_MEANMOTION));

            // Column 64-68
            string revStr = line2.Substring(TLE2_COL_REVATEPOCH, TLE2_LEN_REVATEPOCH).TrimStart();
            if (string.IsNullOrEmpty(revStr)) { revStr = "0"; }
            RevAtEpoch = ParseInt(revStr);

            // To Radians
            InclinationRad = InclinationDeg.ToRadians();
            RAANodeRad = RAANodeDeg.ToRadians();
            ArgPerigeeRad = ArgPerigeeDeg.ToRadians();
            MeanAnomalyRad = MeanAnomalyDeg.ToRadians();
        }
        #endregion

        #region Utility

        /// <summary>
        /// Converts TLE-style exponential notation of the form 
        ///       [ |+|-]00000[ |+|-]0
        /// to decimal notation. Assumes implied decimal point to the left
        /// of the first number in the string, i.e., 
        ///       " 12345-3" =  0.00012345
        ///       "-23429-5" = -0.0000023429   
        ///       " 40436+1" =  4.0436
        /// No sign character implies a positive value, i.e.,
        ///       " 00000 0" =  0.00000
        ///       " 31416 1" =  3.1416
        /// </summary>
        private static string ExpToDecimal(string str)
        {
            const int COL_SIGN = 0;
            const int LEN_SIGN = 1;

            const int COL_MANTISSA = 1;
            const int LEN_MANTISSA = 5;

            const int COL_EXPONENT = 6;
            const int LEN_EXPONENT = 2;

            string sign = str.Substring(COL_SIGN, LEN_SIGN);
            string mantissa = str.Substring(COL_MANTISSA, LEN_MANTISSA);
            string exponent = str.Substring(COL_EXPONENT, LEN_EXPONENT).TrimStart();

            double val = Parse(sign + "0." + mantissa + "e" + exponent);
            int sigDigits = mantissa.Length + Math.Abs(ParseInt(exponent));

            return val.ToString("F" + sigDigits, CultureInfo.InvariantCulture);
        }

        public static string ToTleExponential(double value)
        {
            if (value == 0)
                return " 00000-0";

            // Always work with absolute value first
            double absVal = Math.Abs(value);

            // Compute exponent such that mantissa = round(value * 10^(5 + exponent))
            int exponent = -(int)Math.Floor(Math.Log10(absVal));  // target mantissa: 5 digits

            // Compute scaled mantissa
            double scale = Math.Pow(10, exponent + 4);
            int mantissa = (int)Math.Round(absVal * scale);

            // Handle mantissa overflow (e.g. rounding 99999.9 to 100000)
            if (mantissa >= 100000)
            {
                mantissa /= 10;
                exponent -= 1;
            }

            // Format output
            string signChar = value >= 0 ? " " : "-";
            return $"{signChar}{mantissa:D5}-{exponent-1}";
        }
        #endregion
    }
}