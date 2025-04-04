using LibChromeDotNet.CDP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeApplication
{
    public interface IBrowser
    {
        public Process ChromeProcess { get; }
        public ICDPRemoteHost CDPTarget { get; }
    }

    public class Browser : IBrowser
    {
        public Process ChromeProcess { get; }
        public ICDPRemoteHost CDPTarget { get; }

        public Browser(Process chromeProcess, ICDPRemoteHost cdpTarget)
        {
            ChromeProcess = chromeProcess;
            CDPTarget = cdpTarget;
        }
    }
}
