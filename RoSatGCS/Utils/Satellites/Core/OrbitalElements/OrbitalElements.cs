//
// OrbitalElements.cs
//
// Copyright (c) 2021 Michael F. Henry
// Version 07/2021
//

namespace RoSatGCS.Utils.Satellites.Core
{
    public abstract class OrbitalElements
    {
        private string satelliteName = "";
        private string noradIdStr = "";
        private string intlDesignatorStr = "";
        private Julian epoch = new();

        public double InclinationDeg { get; protected set; }        
        public double InclinationRad { get; protected set; }
        public double Eccentricity { get; protected set; }
        public double RAANodeDeg { get; protected set; }
        public double RAANodeRad { get; protected set; }
        public double ArgPerigeeDeg { get; protected set; }
        public double ArgPerigeeRad { get; protected set; }
        public double MeanAnomalyDeg { get; protected set; }
        public double MeanAnomalyRad { get; protected set; }
        public double BStar { get; protected set; }
        public double MeanMotion { get; protected set; }
        public double MeanMotionDt { get; protected set; }
        public double MeanMotionDt2 { get; protected set; }

        public int RevAtEpoch { get; protected set; }
        public int SetNumber { get; protected set; }

        public string SatelliteName { get => satelliteName; protected set => satelliteName = value; }
        public string NoradIdStr { get => noradIdStr; protected set => noradIdStr = value; }
        public string IntlDesignatorStr { get => intlDesignatorStr; protected set => intlDesignatorStr = value; }

        public Julian Epoch { get => epoch; protected set => epoch = value; }
    }
}
