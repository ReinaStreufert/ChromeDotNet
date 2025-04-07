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
        public event Action? PageLoaded;
        public Task DetachAsync();
        public Task ClosePageAsync();
        public Task ReloadPageAsync();
        public Task NavigatePageAsync(string url);
        public Task NavigatePageAsync(Uri url);
        public Task<IDOMNode> GetDOMDocumentAsync();
        public Task<IJSObject> RequireModuleAsync(IJSModule module);
        public Task<IJSValue> EvaluateExpressionAsync(string jsExpression);
        public Task RequestAsync(ICDPRequest request);
        public Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> request);
        public void SubscribeEvent<TParams>(ICDPEvent<TParams> targetEvent, Action<TParams> handlerCallback);
    }

    public interface IInteropObject
    {
        public IInteropSession Session { get; }
    }
}
