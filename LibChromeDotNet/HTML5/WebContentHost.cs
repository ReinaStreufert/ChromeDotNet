using System;
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
        public Guid Uuid => _Uuid;

        private int _Port = -1;
        private Guid _Uuid = Guid.NewGuid();
        private HttpListener _Listener = new HttpListener();
        private Random _Rand = new Random();
        private CancellationTokenSource _CancelSource = new CancellationTokenSource();

        public Uri GetContentUri(string contentPath)
        {
            if (!contentPath.StartsWith("/"))
                contentPath = "/" + contentPath;
            return new Uri($"http://localhost:{_Port}/{_Uuid}/");
        }

        public async Task ListenAsync(IWebContent webContentSource)
        {
            for (; ;)
            {
                _Port = _Rand.Next(ushort.MaxValue);
                try
                {
                    _Listener.Prefixes.Add($"http://localhost:{_Port}/{_Uuid}/");
                    _Listener.Start();
                    break;
                } catch (HttpListenerException)
                {
                    _Listener = new HttpListener();
                }
            }
            await ServeWebContentAsync(_Listener, _Uuid, webContentSource, _CancelSource.Token);
        }

        public void Stop()
        {
            _CancelSource.Cancel();
            _Listener.Stop();
        }

        private static async Task ServeWebContentAsync(HttpListener listener, Guid uuid, IWebContent webContentSource, CancellationToken cancelToken)
        {
            for (; ;)
            {
                HttpListenerContext requestContext;
                try
                {
                    requestContext = await listener.GetContextAsync();
                } catch (HttpListenerException e)
                {
                    if (cancelToken.IsCancellationRequested)
                        return;
                    throw e;
                }
                var url = requestContext.Request.Url;
                if (url == null) // why is Url nullable?
                    continue;
                if (url.Segments.Length < 2 || !url.Segments[1].StartsWith(uuid.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    continue;
                var resourcePath = string.Concat(url.Segments.Skip(2));
                IContentSource? webContent;
                if (resourcePath == "/" || resourcePath == string.Empty)
                    webContent = await webContentSource.GetIndexResourceAsync();
                else
                    webContent = await webContentSource.GetResourceAsync(resourcePath);
                var response = requestContext.Response;
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
    }
}
