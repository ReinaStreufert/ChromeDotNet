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
        public event Action? PageLoaded;
        public event Action? Detached;
        public IInteropSocket Socket { get; }
        public IInteropTarget SessionTarget { get; }
        public Task DetachAsync();
        public Task ClosePageAsync();
        public Task ReloadPageAsync();
        public Task NavigatePageAsync(string url);
        public Task NavigatePageAsync(Uri url);
        public Task<IDOMNode> GetDOMDocumentAsync();
        public Task<IJSObject> RequireModuleAsync(IJSModule module);
        public Task<IJSValue> EvaluateExpressionAsync(string jsExpression);
        public Task AddJSBindingAsync(string globalName, Action<string> callback);
        public Task RequestAsync(ICDPRequest request);
        public Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> request);
        public ICDPSubscription SubscribeEvent<TParams>(ICDPEvent<TParams> targetEvent, Action<TParams> handlerCallback);
    }

    public interface IInteropObject
    {
        public IInteropSession Session { get; }
    }
}
