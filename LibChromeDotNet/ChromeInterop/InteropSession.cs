using LibChromeDotNet.CDP;
using LibChromeDotNet.CDP.Domains;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class InteropSession : IInteropSession
    {
        public IInteropSocket Socket => _InteropSocket;
        public IInteropTarget SessionTarget => _Target;
        public event Action? PageLoaded;
        public event Action? Detached;

        public InteropSession(IInteropSocket interopSocket, IInteropTarget target, ICDPSocket cdpSocket, string sessionId)
        {
            _InteropSocket = interopSocket;
            _Target = target;
            _CDP = cdpSocket;
            _SessionId = sessionId;
            _Subscriptions.Add(SubscribeEvent(Page.DOMContentLoaded, t => PageLoaded?.Invoke()));
            _Subscriptions.Add(_CDP.SubscribeEvent(Target.DetachedFromTarget, s =>
            {
                if (s == _SessionId)
                {
                    FreeSession();
                    Detached?.Invoke();
                }
            }));
        }

        private IInteropSocket _InteropSocket;
        private IInteropTarget _Target;
        private ICDPSocket _CDP;
        private List<ICDPSubscription> _Subscriptions = new List<ICDPSubscription>();
        private string _SessionId;
        private ConcurrentDictionary<string, Task<IJSObject>> _LoadedModules = new ConcurrentDictionary<string, Task<IJSObject>>();

        public async Task ClosePageAsync()
        {
            await RequestAsync(Page.Close);
            FreeSession();
        }

        public async Task ReloadPageAsync() => await RequestAsync(Page.Reload);

        public Task NavigatePageAsync(string url) => NavigatePageAsync(new Uri(url))!;

        public async Task NavigatePageAsync(Uri url)
        {
            await RequestAsync(Page.Navigate(url));
        }

        public async Task DetachAsync()
        {
            await RequestAsync(Target.DetachFromTarget(_SessionId));
        }

        public async Task<IDOMNode> GetDOMDocumentAsync()
        {
            var nodeTree = await RequestAsync(DOM.GetDocumentTree());
            return new DOMNode(this, nodeTree);
        }

        public Task<IJSObject> RequireModuleAsync(IJSModule module)
        {
            return _LoadedModules.GetOrAdd(module.Name, o => LoadJSModuleAsync(module));
        }

        public async Task<IJSValue> EvaluateExpressionAsync(string jsExpression)
        {
            var remoteObject = await RequestAsync(Runtime.Evaluate(jsExpression));
            return IJSValue.FromRemoteObject(this, remoteObject);
        }

        private async Task<IJSObject> LoadJSModuleAsync(IJSModule module)
        {
            var jsSource = await module.GetScriptSourceAsync();
            var remoteObject = await RequestAsync(Runtime.Evaluate($"(function() {{{jsSource}{Environment.NewLine}}} )();"));
            return new JSObject(this, remoteObject.ObjectId ?? throw new InvalidOperationException("Module script did not return an object"));
        }

        public async Task AddJSBindingAsync(string name, Action<string> callback)
        {
            SubscribeEvent(Runtime.BindingCalled, e =>
            {
                if (e.BindingName == name)
                    callback(e.CallParam);
            });
            await RequestAsync(Runtime.AddBinding(name));
        }

        public Task RequestAsync(ICDPRequest request) => _CDP.RequestAsync(request, _SessionId);
        public Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> request) => _CDP.RequestAsync(request, _SessionId);
        public ICDPSubscription SubscribeEvent<TParams>(ICDPEvent<TParams> targetEvent, Action<TParams> handlerCallback) => _CDP.SubscribeEvent(targetEvent, handlerCallback, _SessionId);
    
        private void FreeSession()
        {
            foreach (var subscription in _Subscriptions)
                subscription.Unsubscribe();
        }
    }
}
