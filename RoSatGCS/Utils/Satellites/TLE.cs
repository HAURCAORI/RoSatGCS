using RoSatGCS.Utils.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using RoSatGCS.Utils.Satellites.Core;

namespace RoSatGCS.Utils.Satellites
{
    public class TLE
    {
        private string name = "";
        private string line1 = "";
        private string line2 = "";

        private readonly TwoLineElements mTLE;

        public TLE(string tle) : this(tle.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) { }

        public TLE(string name, string line1, string line2) : this(new string[] { name, line1, line2 }) { }

        public TLE(string[] tle)
        {
            if (tle.Length != 3)
            {
                throw new SatellitesTleException();
            }

            try
            {
                mTLE = new TwoLineElements(tle[0], tle[1], tle[2]);
                this.name = tle[0];
                this.line1 = tle[1];
                this.line2 = tle[2];
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new SatellitesTleException();
            }
            catch (ArgumentNullException)
            {
                throw new SatellitesTleException();
            }
            catch (FormatException)
            {
                throw new SatellitesTleException();
            }
            catch (OverflowException)
            {
                throw new SatellitesTleException();
            }
        }


        public OrbitalElements OrbitalElements
        {
            get => mTLE;
        }
    }
}
