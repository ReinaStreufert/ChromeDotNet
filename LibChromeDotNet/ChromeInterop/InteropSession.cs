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

        public async Task DetachAsync()
        {
            await _CDP.RequestAsync(Target.DetachFromTarget(_SessionId));
        }

        public async Task<IDOMNode> GetDOMDocumentAsync()
        {
            var nodeTree = await RequestAsync(DOM.GetDocumentTree());
            return new DOMNode(this, nodeTree);
        }

        public async Task<IFrame> GetFrameTreeAsync()
        {
            var frameTree = await RequestAsync(Page.GetFrameTree);
            return new Frame(this, frameTree);
        }

        public Task<IJSObject> RequireJSModuleAsync(IJSModule module)
        {
            return _LoadedModules.GetOrAdd(module.Key, o => LoadJSModuleAsync(module));
        }

        private async Task<IJSObject> LoadJSModuleAsync(IJSModule module)
        {
            var jsSource = await module.GetScriptSourceAsync();
            var remoteObject = await RequestAsync(Runtime.Evaluate($"(function(){{{jsSource}}})();"));
            return new JSObject(this, remoteObject);
        }

        public Task RequestAsync(ICDPRequest request) =>
            _CDP.RequestAsync(new SessionRequest(request, _SessionId));
        public Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> request) =>
            _CDP.RequestAsync(new SessionRequest<TResult>(request, _SessionId));
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
