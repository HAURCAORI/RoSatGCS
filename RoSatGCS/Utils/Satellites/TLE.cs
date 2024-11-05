using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Zeptomoby.OrbitTools;

namespace RoSatGCS.Utils.Satellites
{
    internal class TLE
    {
        private string name;
        private string line1;
        private string line2;

        private readonly TwoLineElements? mTLE;
        public TLE(string tle)
        {
            string[] slice = tle.Split('\r');
            if (slice.Length != 3)
            {
                name = line1 = line2 = "";
                return;
            }
            name = slice[0];
            line1 = slice[1];
            line2 = slice[2];
            
        }

        public TLE(string name, string line1, string line2)
        {
            this.name = name;
            this.line1 = line1;
            this.line2 = line2;
            mTLE = new TwoLineElements(name, line1, line2);
        }

        public OrbitalElements OrbitalElements
        {
            get => mTLE;
        }
    }
}
