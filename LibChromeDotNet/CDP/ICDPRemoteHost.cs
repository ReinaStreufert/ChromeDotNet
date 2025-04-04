using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    public interface ICDPRemoteHost
    {
        public Uri RemoteWSHost { get; }
        public string ChromeVersion { get; }
    }

    public class CDPRemoteHost : ICDPRemoteHost
    {
        public Uri RemoteWSHost { get; }
        public string ChromeVersion { get; }

        public CDPRemoteHost(Uri remoteWSHost, string chomeVersion)
        {
            RemoteWSHost = remoteWSHost;
            ChromeVersion = chomeVersion;
        }
    }
}
