using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Utils.Maps
{
    public class MapControl : GMapControl
    {
        public MapControl() {
            this.MapProvider = GMapProviders.GoogleMap;
            //this.MinZoom = 4;
            //this.MaxZoom = 17;
            this.Zoom = 13;

        }
    }
}
