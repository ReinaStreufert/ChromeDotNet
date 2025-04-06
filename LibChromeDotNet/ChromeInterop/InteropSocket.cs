using LibChromeDotNet.CDP;
using LibChromeDotNet.CDP.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class InteropSocket : IInteropSocket
    {
        private ICDPSocket _CDP;

        public InteropSocket(ICDPSocket cdpSocket)
        {
            _CDP = cdpSocket;
        }

        public void Dispose() => _CDP.Dispose();
        public Task CloseAsync() => _CDP.CloseAsync();

        public async Task EnableTargetDiscoveryAsync(Action<IInteropTarget> tarqetCreated, Action<string> targetDestroyed)
        {
            _CDP.SubscribeEvent(Target.OnTargetCreated, targetInfo => tarqetCreated(targetInfo));
            _CDP.SubscribeEvent(Target.OnTargetDestroyed, targetDestroyed);
            await _CDP.RequestAsync(Target.SetDiscoverTargets(true));
        }

        public async Task<IEnumerable<IInteropTarget>> GetTargetsAsync()
        {
            return (await _CDP.RequestAsync(Target.GetTargets()))
                .Cast<IInteropTarget>();
        }

        public async Task<IInteropSession> OpenSessionAsync(IInteropTarget target, IEnumerable<IURIFetchHandler> handlers)
        {
            var sessionId = await _CDP.RequestAsync(Target.AttachToTarget(target.Id));
            var session = new InteropSession(this, target, _CDP, sessionId);
            _CDP.SubscribeEvent(Fetch.OnRequestPaused, requestPausedEvent =>
            {
                var requestUrl = requestPausedEvent.Request.RequestUri.ToString();
                var matchingHandlers = handlers
                    .Where(h => requestUrl.StartsWith(h.UriPattern));
                var fetchContext = new ResourceFetchContext(session, requestPausedEvent.ResourceType, requestPausedEvent.Request, requestPausedEvent.Id);
                foreach (var handler in matchingHandlers)
                    _ = handler.HandleAsync(fetchContext);
            });
            var uriPatterns = handlers
                .Select(h => h.UriPattern);
            await session.RequestAsync(Fetch.Enable(uriPatterns));
            await session.RequestAsync(Runtime.Enable);
            return session;
        }

        public Task<IInteropSession> OpenSessionAsync(IInteropTarget target, params IURIFetchHandler[] handlers) => OpenSessionAsync(target, (IEnumerable<IURIFetchHandler>)handlers);

        private class ResourceFetchContext : IResourceFetchContext
        {
            private IInteropSession _Session;
            private FetchResourceType _ResourceType;
            private IHttpRequestInfo _RequestInfo;
            private string _RequestId;

            public ResourceFetchContext(IInteropSession session, FetchResourceType resourceType, IHttpRequestInfo requestInfo, string requestId)
            {
                _Session = session;
                _ResourceType = resourceType;
                _RequestInfo = requestInfo;
                _RequestId = requestId;
            }

            public FetchResourceType ResourceType => _ResourceType;
            public IHttpRequestInfo Request => _RequestInfo;

            public async Task ContinueRequestAsync() =>
                await _Session.RequestAsync(Fetch.ContinueRequest(_RequestId));

            public async Task FailRequestAsync(FailReason reason = FailReason.Failed) =>
                await _Session.RequestAsync(Fetch.FailRequest(_RequestId, reason));

            public async Task FulfillRequestAsync(int responseCode, Action<IFetchHandlerHttpResponse> responseCallback)
            {
                var response = new HttpResponse(_Session, _RequestId, responseCode);
                responseCallback(response);
                await response.SendAsync();
            }
        }

        private class HttpResponse : IFetchHandlerHttpResponse
        {
            public int ResponseCode { get; }

            private IInteropSession _Session;
            private string _RequestId;
            private Dictionary<string, string> _Headers = new Dictionary<string, string>();
            private byte[]? _ResponseData;

            public HttpResponse(IInteropSession session, string requestId, int responseCode)
            {
                ResponseCode = responseCode;
                _Session = session;
                _RequestId = requestId;
            }

            public void SetBody(byte[] responseData)
            {
                _ResponseData = responseData;
            }

            public void SetHeader(string key, string value)
            {
                _Headers[key] = value;
            }

            public async Task SendAsync()
            {
                await _Session.RequestAsync(Fetch.FulfillRequest(_RequestId, ResponseCode, _Headers, _ResponseData));
            }
        }
    }
}
