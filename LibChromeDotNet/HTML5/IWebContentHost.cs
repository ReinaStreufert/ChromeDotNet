using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public interface IWebContentHost
    {
        public Task ListenAsync(IWebContent webContentSource);
        public void Stop();
        public int LoopbackPort { get; }
        public IWebContentProvider CreateContentProvider();
    }

    // uhhhhhhh okay so CDP's Target.createTarget i couldve sworn when i first tried it inherited app mode from the command line arguments of
    // the original browser launch but i must have remembered wrong. now i have to use window.open from js which is fine i guess except it's
    // ambiguous which new CDP target corresponds to which attempt to create a target, in the case theyre created quickly.
    // so now im doing all this crap to make sure they can't get mixed up...
    public interface IWebContentProvider : IDisposable
    {
        public Guid Uuid { get; }
        public Uri GetContentUri(string contentPath);
    }
}
