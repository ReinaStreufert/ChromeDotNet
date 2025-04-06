using LibChromeDotNet.ChromeInterop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP.Domains
{
    public static class Fetch
    {
        private static ICDPEvent<RequestPausedEvent>? _OnRequestPaused;

        public static ICDPEvent<RequestPausedEvent> OnRequestPaused => _OnRequestPaused ?? CreateOnRequestPaused();

        public static ICDPRequest Enable(IEnumerable<string> patterns)
        {
            var jsonParams = new JObject();
            if (patterns.Any())
            {
                var patternArray = new JArray();
                foreach (var pattern in patterns)
                {
                    var patternObject = new JObject();
                    patternObject.Add("urlPattern", pattern);
                    patternArray.Add(patternObject);
                }
                jsonParams.Add("patterns", patternArray);
            }
            return CDP.Request("Fetch.enable", jsonParams);
        }

        public static ICDPRequest Disable => CDP.Request("Fetch.disable");

        public static ICDPRequest ContinueRequest(string requestId)
        {
            var jsonParams = new JObject();
            jsonParams.Add("requestId", requestId);
            return CDP.Request("Fetch.continueRequest", jsonParams);
        }

        public static ICDPRequest FailRequest(string requestId, FailReason reason)
        {
            var jsonParams = new JObject();
            jsonParams.Add("requestId", requestId);
            jsonParams.Add("errorReason", reason.ToString());
            return CDP.Request("Fetch.failRequest", jsonParams);
        }

        public static ICDPRequest FulfillRequest(string requestId, int responseCode, IEnumerable<KeyValuePair<string, string>> headers, byte[]? body = null)
        {
            var jsonParams = new JObject();
            jsonParams.Add("requestId", requestId);
            jsonParams.Add("responseCode", responseCode);
            if (headers.Any())
            {
                var headerArray = new JArray();
                foreach (var header in headers)
                {
                    var headerEntry = new JObject();
                    headerEntry.Add("name", header.Key);
                    headerEntry.Add("value", header.Value);
                    headerArray.Add(headerEntry);
                }
                jsonParams.Add("responseHeaders", headerArray);
            }
            if (body != null)
                jsonParams.Add("body", Convert.ToBase64String(body));
            return CDP.Request("Fetch.fulfillRequest", jsonParams);
        }

        private static ICDPEvent<RequestPausedEvent> CreateOnRequestPaused()
        {
            _OnRequestPaused = CDP.Event("Fetch.requestPaused", jsonParams => new RequestPausedEvent(jsonParams));
            return _OnRequestPaused;
        }
    }

    public struct RequestPausedEvent
    {
        public string Id;
        public int FrameId;
        public FetchResourceType ResourceType;
        public HttpRequestInfo Request;

        public RequestPausedEvent(JObject jsonParams)
        {
            Id = jsonParams["requestId"]!.ToString();
            FrameId = (int)jsonParams["frameId"]!;
            ResourceType = Enum.Parse<FetchResourceType>(jsonParams["resourceType"]!.ToString());
            Request = new HttpRequestInfo((JObject)jsonParams["request"]!);
        }
    }

    public struct HttpRequestInfo : IHttpRequestInfo
    {
        public Uri RequestUri;
        public string HttpMethod;
        public KeyValuePair<string, string>[] Headers;

        public HttpRequestInfo(JObject jsonRequest)
        {
            RequestUri = new Uri(jsonRequest["url"]!.ToString());
            HttpMethod = jsonRequest["method"]!.ToString();
            Headers = ((JObject)jsonRequest["headers"]!)
                .Properties()
                .Select(p => new KeyValuePair<string, string>(p.Name, p.Value.ToString()))
                .ToArray();
        }

        Uri IHttpRequestInfo.RequestUri => RequestUri;
        string IHttpRequestInfo.HttpMethod => HttpMethod;
        KeyValuePair<string, string>[] IHttpRequestInfo.Headers => Headers;
    }
}
