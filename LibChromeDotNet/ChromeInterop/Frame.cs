using LibChromeDotNet.CDP.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class Frame : IFrame
    {
        private InteropSession _Session;
        private FrameTree _Tree;
        private FrameInfo _FrameInfo => _Tree.Frame;

        public Frame(InteropSession session, FrameTree tree)
        {
            _Session = session;
            _Tree = tree;
        }

        public Uri NavigationUrl => _FrameInfo.NavigationUri;
        public IEnumerable<IFrame> Children => _Tree.Children
            .Select(t => new Frame(_Session, t));

        public async Task<IJSExecutionContext> CreateJSContextAsync(string isolatedWorldName)
        {
            var executionContextId = await _Session.RequestAsync(Page.CreateIsolatedWorld(_FrameInfo.Id, isolatedWorldName));
            return new JSExecutionContext(executionContextId, isolatedWorldName);
        }

        public async Task NavigateAsync(Uri url)
        {
            await _Session.RequestAsync(Page.Navigate(url, _FrameInfo.Id));
        }
    }
}
