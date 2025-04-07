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
        private CancellationTokenSource _ListenerCancelSource = new CancellationTokenSource();

        public Uri GetContentUri(string contentPath)
        {
            if (!contentPath.StartsWith("/"))
                contentPath = "/" + contentPath;
            return new Uri($"http://localhost:{_Port}/{_Uuid}/");
        }

        public void StartHttpListener(IWebContent webContentSource)
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
                    _Listener.Prefixes.Clear();
                }
            }
            _ = ServeWebContentAsync(_Listener, webContentSource, _ListenerCancelSource.Token);
        }

        private static async Task ServeWebContentAsync(HttpListener listener, IWebContent webContentSource, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                var requestContext = await listener.GetContextAsync();
                var url = requestContext.Request.Url;
                if (url == null) // why is Url nullable?
                    continue;
                var resourcePath = url.AbsolutePath;
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
                    var contentStream = webContent.GetContentStream();
                    contentStream.CopyTo(response.OutputStream);
                }
                response.Close();
            }
        }

        public void StopListening()
        {
            _ListenerCancelSource.Cancel();
            _Listener.Stop();
        }
    }
}
