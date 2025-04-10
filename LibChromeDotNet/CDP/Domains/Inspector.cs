using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP.Domains
{
    public static class Inspector
    {
        public static ICDPEvent<string> Detached => CDP.Event("Inspector.detached", json => json["reason"]!.ToString());
    }
}
