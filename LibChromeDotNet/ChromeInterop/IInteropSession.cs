using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibChromeDotNet.CDP;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IInteropSession
    {
        public IInteropSocket Socket { get; }
        public IInteropTarget SessionTarget { get; }
        public Task DetachAsync();
        public Task ClosePageAsync();
        public Task<IFrame> GetFrameTreeAsync();
        public Task<IDOMNode> GetDOMDocumentAsync();
        public Task<IJSObject> RequireJSModuleAsync(IJSModule module);
        public Task RequestAsync(ICDPRequest request);
        public Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> request);
        public void SubscribeEvent<TParams>(ICDPEvent<TParams> targetEvent, Action<TParams> handlerCallback);
    }
}
