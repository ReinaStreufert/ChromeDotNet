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

        public InteropSession(IInteropSocket interopSocket, IInteropTarget target, ICDPSocket cdpSocket, string sessionId)
        {
            _InteropSocket = interopSocket;
            _Target = target;
            _CDP = cdpSocket;
            _SessionId = sessionId;
        }

        private IInteropSocket _InteropSocket;
        private IInteropTarget _Target;
        private ICDPSocket _CDP;
        private string _SessionId;
        private ConcurrentDictionary<object, Task<IJSObject>> _LoadedModules = new ConcurrentDictionary<object, Task<IJSObject>>();

        public async Task ClosePageAsync()
        {
            await RequestAsync(Page.Close);
        }

        public Task NavigatePageAsync(string url) => NavigatePageAsync(new Uri(url))!;

        public async Task NavigatePageAsync(Uri url)
        {
            await RequestAsync(Page.Navigate(url));
        }

        public async Task DetachAsync()
        {
            await _CDP.RequestAsync(Target.DetachFromTarget(_SessionId));
        }

        public async Task<IDOMNode> GetDOMDocumentAsync()
        {
            var nodeTree = await RequestAsync(DOM.GetDocumentTree());
            return new DOMNode(this, nodeTree);
        }

        public Task<IJSObject> RequireModuleAsync(IJSModule module)
        {
            return _LoadedModules.GetOrAdd(module.Key, o => LoadJSModuleAsync(module));
        }

        public async Task<IJSValue> EvaluateExpressionAsync(string jsExpression)
        {
            var remoteObject = await RequestAsync(Runtime.Evaluate(jsExpression));
            return IJSValue.FromRemoteObject(this, remoteObject);
        }

        private async Task<IJSObject> LoadJSModuleAsync(IJSModule module)
        {
            var jsSource = await module.GetScriptSourceAsync();
            var remoteObject = await RequestAsync(Runtime.Evaluate($"(function(){{{jsSource}}})();"));
            return new JSObject(this, remoteObject.ObjectId ?? throw new InvalidOperationException("Module script did not return an object"));
        }

        public Task RequestAsync(ICDPRequest request) => _CDP.RequestAsync(request, _SessionId);
        public Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> request) => _CDP.RequestAsync(request, _SessionId);
        public void SubscribeEvent<TParams>(ICDPEvent<TParams> targetEvent, Action<TParams> handlerCallback) =>
            _CDP.SubscribeEvent(targetEvent, handlerCallback);

        private class SessionRequest : ICDPRequest
        {
            private ICDPRequest _BaseRequest;
            private string _SessionId;

            public SessionRequest(ICDPRequest baseRequest, string sessionId)
            {
                _BaseRequest = baseRequest;
                _SessionId = sessionId;
            }

            public string MethodName => _BaseRequest.MethodName;

            public JObject GetJsonParams()
            {
                var jsonParams = _BaseRequest.GetJsonParams();
                jsonParams.Add("sessionId", _SessionId);
                return jsonParams;
            }
        }

        private class SessionRequest<TResult> : ICDPRequest<TResult>
        {
            private ICDPRequest<TResult> _BaseRequest;
            private string _SessionId;

            public SessionRequest(ICDPRequest<TResult> baseRequest, string sessionId)
            {
                _BaseRequest = baseRequest;
                _SessionId = sessionId;
            }

            public string MethodName => _BaseRequest.MethodName;

            public JObject GetJsonParams()
            {
                var jsonParams = _BaseRequest.GetJsonParams();
                jsonParams.Add("sessionId", _SessionId);
                return jsonParams;
            }

            public TResult GetResultFromJson(JObject resultObject) => _BaseRequest.GetResultFromJson(resultObject);
        }
    }
}
