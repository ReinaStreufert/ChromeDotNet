using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IFrame
    {
        public Uri NavigationUrl { get; }
        public IEnumerable<IFrame> Children { get; }
        public Task<IJSExecutionContext> CreateJSContextAsync(string isolatedWorldName);
        public Task NavigateAsync(Uri url);
    }
}
