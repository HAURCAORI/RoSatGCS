using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Utils.Exception
{
    [Serializable]
    public class InvalidFunctionFileException : System.Exception
    {
        public InvalidFunctionFileException() { }

        public InvalidFunctionFileException(string message) : base(message) { }

        public InvalidFunctionFileException(string message, System.Exception innerException) : base(message, innerException) { }
    }
}
