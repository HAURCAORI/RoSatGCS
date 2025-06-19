using RoSatGCS.Utils.Satellites.Core;

namespace RoSatGCS.Utils.Exception
{
    [Serializable]
    public sealed class SatellitesDecayException : System.Exception
    {
        /// <summary>
        /// The GMT when the satellite orbit decays.
        /// </summary>
        public DateTime DecayTime { get; private set; }

        /// <summary>
        /// The name of the satellite whose orbit decayed.
        /// </summary>
        public string SatelliteName { get; private set; }

        public SatellitesDecayException() { }
        public SatellitesDecayException(string message) : base(message) { }
        public SatellitesDecayException(string message, System.Exception inner) : base(message, inner) { }

        public SatellitesDecayException(Julian decayTime, string satelliteName)
           : this(decayTime.ToTime(), satelliteName)
        {
        }

        public SatellitesDecayException(DateTime decayTime, string satelliteName)
        {
            DecayTime = decayTime;
            SatelliteName = satelliteName;
        }
    }
}
