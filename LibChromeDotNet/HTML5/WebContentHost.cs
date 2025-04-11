using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public class WebContentHost : IWebContentHost
    {
        public int LoopbackPort => _Port > 0 ? _Port : throw new InvalidOperationException("Http listener isnt started");

        private int _Port = -1;
        private HttpListener _Listener = new HttpListener();
        private Random _Rand = new Random();
        private CancellationTokenSource _CancelSource = new CancellationTokenSource();
        private ConcurrentDictionary<Guid, WebContentProvider> _ContentProviderDict = new ConcurrentDictionary<Guid, WebContentProvider>();
        
        public async Task ListenAsync(IWebContent webContentSource)
        {
            for (; ;)
            {
                _Port = _Rand.Next(ushort.MaxValue);
                try
                {
                    _Listener.Prefixes.Add($"http://localhost:{_Port}/");
                    _Listener.Start();
                    break;
                } catch (HttpListenerException)
                {
                    _Listener = new HttpListener();
                }
            }
            await ServeWebContentAsync(webContentSource);
        }

        public void Stop()
        {
            _CancelSource.Cancel();
            _Listener.Stop();
        }

        public IWebContentProvider CreateContentProvider()
        {
            var provider = new WebContentProvider(Guid.NewGuid(), this);
            _ContentProviderDict.TryAdd(provider.Uuid, provider); // will succeed bc uuids dont collide unless uhh mercury is in retrograde or something dumb as fuck like that.
            return provider;
        }

        private async Task ServeWebContentAsync(IWebContent webContentSource)
        {
            var cancelToken = _CancelSource.Token;
            for (; ;)
            {
                HttpListenerContext requestContext;
                try
                {
                    requestContext = await _Listener.GetContextAsync();
                } catch (HttpListenerException e)
                {
                    if (cancelToken.IsCancellationRequested)
                        return;
                    throw e;
                }
                var response = requestContext.Response;
                var url = requestContext.Request.Url;
                if (url == null || url.Segments.Length < 2)
                {
                    FailNotFound(response);
                    continue;
                }
                var uriProviderUuid = new Guid(url.Segments[1].TrimEnd('/'));
                if (!_ContentProviderDict.TryGetValue(uriProviderUuid, out var provider))
                {
                    FailNotFound(response);
                    continue;
                }
                var resourcePath = string.Concat(url.Segments.Skip(2));
                IContentSource? webContent;
                if (resourcePath == "/" || resourcePath == string.Empty)
                    webContent = await webContentSource.GetIndexResourceAsync();
                else
                    webContent = await webContentSource.GetResourceAsync(resourcePath);
                
                if (webContent == null)
                {
                    response.StatusCode = 404; // 404 not found
                    continue;
                } else
                {
                    response.StatusCode = 200; // 200 okay
                    response.ContentType = webContent.MimeType;
                    webContent.WriteToStream(response.OutputStream);
                }
                response.Close();
            }
        }

        private void FailNotFound(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.Close();
        }

        private class WebContentProvider : IWebContentProvider
        {
            public Guid Uuid => _Uuid;

            public WebContentProvider(Guid uuid, WebContentHost host)
            {
                _Uuid = uuid;
                _Host = host;
            }

            private Guid _Uuid;
            private WebContentHost _Host;

            public Uri GetContentUri(string contentPath)
            {
                if (!contentPath.StartsWith("/"))
                    contentPath = "/" + contentPath;
                return new Uri($"http://localhost:{_Host._Port}/{_Uuid}" + contentPath);
            }

            public void Dispose()
            {
                _Host._ContentProviderDict.TryRemove(_Uuid, out _);
            }
        }
    }
}
