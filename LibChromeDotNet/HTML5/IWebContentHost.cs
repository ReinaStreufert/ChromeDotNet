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
        public Guid Uuid { get; }
        public Uri GetContentUri(string contentPath);
    }
}
