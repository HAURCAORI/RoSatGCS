using RoSatGCS.Utils.Satellites.Core;

namespace RoSatGCS.Utils.Exception
{
    [Serializable]
    public sealed class SatellitesDataException : System.Exception
    {
        public SatellitesDataException() { }
        public SatellitesDataException(string message) : base(message) { }
        public SatellitesDataException(string message, System.Exception inner) : base(message, inner) { }

    }
}
