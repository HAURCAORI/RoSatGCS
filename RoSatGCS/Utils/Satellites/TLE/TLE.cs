using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoSatGCS.Utils.Exception;
using RoSatGCS.Utils.Satellites.Core;
using System.Globalization;


namespace RoSatGCS.Utils.Satellites.TLE
{
    public class TLE
    {
        private string name = "";
        private string line1 = "";
        private string line2 = "";
        private bool isValid = false;

        public bool IsValid
        {
            get => isValid;
            set => isValid = value;
        }

        private TwoLineElements mTLE;

        public TLE(string tle, bool ignoreCheckSum = false) : this(tle.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries), ignoreCheckSum) { }

        public TLE(string name, string line1, string line2, bool ignoreCheckSum = false) : this(new string[] { name, line1, line2 }, ignoreCheckSum) { }

        public TLE(string[] tle, bool ignoreCheckSum = false)
        {
            if (tle[0] == string.Empty || tle[1] == string.Empty || tle[2] == string.Empty)
            {
                throw new SatellitesTleException("Invalid Line");
            }

            try
            {
                mTLE = new TwoLineElements(tle[0], tle[1], tle[2], ignoreCheckSum);
                name = tle[0];
                line1 = tle[1];
                line2 = tle[2];

                isValid = true;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new SatellitesTleException(ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                throw new SatellitesTleException(ex.Message);
            }
            catch (FormatException ex)
            {
                throw new SatellitesTleException(ex.Message);
            }
            catch (OverflowException ex)
            {
                throw new SatellitesTleException(ex.Message);
            }
        }

        public OrbitalElements OrbitalElements
        {
            get => mTLE;
        }
        public string SatelliteName { get => mTLE.SatelliteName; }
        public string NoradId { get => mTLE.NoradIdStr; }
        public char Class { get => mTLE.Class; }
        public string IntlDesignator { get => mTLE.IntlDesignatorStr; }
        public DateTime Epoch { get => mTLE.Epoch.ToTime(); }
        public double MeanMotionDt { get => mTLE.MeanMotionDt; }
        public double MeanMotionDt2 { get => mTLE.MeanMotionDt2; }
        public double BStar { get => mTLE.BStar; }
        public int SetNumber { get => mTLE.SetNumber; }
        public double InclinationDeg { get => mTLE.InclinationDeg; }
        public double InclinationRad { get => mTLE.InclinationRad; }
        public double RAANodeDeg { get => mTLE.RAANodeDeg; }
        public double RAANodeRad { get => mTLE.RAANodeRad; }
        public double Eccentricity { get => mTLE.Eccentricity; }
        public double ArgPerigeeDeg { get => mTLE.ArgPerigeeDeg; }
        public double ArgPerigeeRad { get => mTLE.ArgPerigeeRad; }
        public double MeanAnomalyDeg { get => mTLE.MeanAnomalyDeg; }
        public double MeanAnomalyRad { get => mTLE.MeanAnomalyRad; }
        public double MeanMotion { get => mTLE.MeanMotion; }
        public int RevAtEpoch { get => mTLE.RevAtEpoch; }


        public static string GenerateLine1(
            int noradId,
            char classification,
            string intlDesignator,
            DateTime epoch,
            double meanMotionDt,
            double meanMotionDt2,
            double bstar,
            int ephemerisType,
            int elementNumber)
        {
            // Epoch
            int epochYear = epoch.Year % 100;
            double dayOfYear = (epoch - new DateTime(epoch.Year, 1, 1)).TotalDays + 1;

            // First time derivative of mean motion (decimal format, 10 chars)
            string mmDt = meanMotionDt.ToString("0.00000000;-.00000000", CultureInfo.InvariantCulture).Replace("0.", " .");


            // Second time derivative of mean motion (scientific format, 8 chars without 'E')
            string mmDt2 = TwoLineElements.ToTleExponential(meanMotionDt2);

            // B* drag term (also scientific format, 8 chars without 'E')
            string bstarStr = TwoLineElements.ToTleExponential(bstar);

            string number = elementNumber.ToString().PadLeft(4, ' ');

            // Construct line 1
            string line = $"1 {noradId:D5}{classification} {intlDesignator.PadRight(8)} {epochYear:D2}{dayOfYear:000.00000000}" +
                          $" {mmDt} {mmDt2} {bstarStr} {ephemerisType} {number}";

            // Pad and compute checksum
            line = line.PadRight(68);
            int checksum = TwoLineElements.ComputeChecksum(line);
            return line + checksum.ToString();
        }

        public static string GenerateLine2(
            int noradId,
            double inclinationDeg,
            double raanDeg,
            double eccentricity,
            double argPerigeeDeg,
            double meanAnomalyDeg,
            double meanMotion,
            int revAtEpoch)
        {
            string eccStr = eccentricity.ToString(".0000000", CultureInfo.InvariantCulture).Substring(1); // remove "0."

            string rev = revAtEpoch.ToString().PadLeft(5, ' ');

            string line = $"2 {noradId:D5} {inclinationDeg,8:##0.0000} {raanDeg,8:##0.0000} {eccStr,7}" +
                          $" {argPerigeeDeg,8:##0.0000} {meanAnomalyDeg,8:##0.0000} {meanMotion,11:0.00000000}{rev}";

            line = line.PadRight(68);
            int checksum = TwoLineElements.ComputeChecksum(line);
            return line + checksum.ToString();
        }

        public override string ToString()
        {
            return $"{name}{Environment.NewLine}{line1}{Environment.NewLine}{line2}";
        }
    }
}
