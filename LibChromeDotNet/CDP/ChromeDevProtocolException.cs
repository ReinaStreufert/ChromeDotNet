using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    public class ChromeDevProtocolException : Exception
    {
        public int ErrorCode { get; }

        public ChromeDevProtocolException(string errorMessage, int errorCode) : base(errorMessage)
        {
            ErrorCode = errorCode;
        }
    }
}
