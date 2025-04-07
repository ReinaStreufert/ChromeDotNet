using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public interface IAppWindow
    {
        public event AsyncWindowEvent? PageLoaded;
        public Task CloseAsync();
        public Task<IDOMNode> GetDocumentBodyAsync();
        public Task NavigateAsync(string contentPath);
    }

    public delegate Task AsyncWindowEvent(IAppWindow window);
}
