namespace RoSatGCS.Utils.Exception
{
    [Serializable]
    public class SatellitesTleException : System.Exception
    {
        public SatellitesTleException() { }

        public SatellitesTleException(string message) : base(message) { }

        public SatellitesTleException(string message, System.Exception innerException) : base(message, innerException) { }

    }
}
