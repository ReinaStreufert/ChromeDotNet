using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IURIFetchHandler
    {
        public string? UriPattern { get; }
        public Task HandleAsync(IResourceFetchContext fetchContext);
    }

    public interface IResourceFetchContext
    {
        public FetchResourceType ResourceType { get; }
        public IHttpRequestInfo Request { get; }
        public Task ContinueRequestAsync();
        public Task FailRequestAsync(FailReason reason = FailReason.Failed);
        public Task FulfillRequestAsync(int responseCode, Action<IFetchHandlerHttpResponse> responseCallback);
    }

    public interface IHttpRequestInfo
    {
        public Uri RequestUri { get; }
        public string HttpMethod { get; }
        public KeyValuePair<string, string>[] Headers { get; }
    }

    public interface IFetchHandlerHttpResponse
    {
        public int ResponseCode { get; }
        public void SetHeader(string key, string value);
        public void SetHeader(string key, ReadOnlySpan<byte> value);
        public void SetBody(byte[] responseData);
    }

    public enum FetchResourceType
    {
        Document,
        Stylesheet, 
        Image, 
        Media, 
        Font, 
        Script, 
        TextTrack,
        XHR,
        Fetch, 
        Prefetch,
        EventSource, 
        WebSocket,
        Manifest, 
        SignedExchange, 
        Ping, 
        CSPViolationReport, 
        Preflight,
        Other
    }

    public enum FailReason
    {
        Failed,
        Aborted,
        TimedOut,
        AccessDenied, 
        ConnectionClosed,
        ConnectionReset, 
        ConnectionRefused,
        ConnectionAborted,
        ConnectionFailed,
        NameNotResolved,
        InternetDisconnected,
        AddressUnreachable,
        BlockedByClient, 
        BlockedByResponse
    }
}
