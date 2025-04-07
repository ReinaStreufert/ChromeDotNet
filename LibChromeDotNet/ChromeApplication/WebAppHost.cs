using LibChromeDotNet.CDP;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeApplication
{
    public class WebAppHost : IWebAppHost
    {
        public static IWebAppHost Create(IWebApp app) => new WebAppHost(ChromeLauncher.CreateForPlatform(), app);

        private WebAppHost(IChromeLauncher launcher, IWebApp app)
        {
            _App = app;
            _Launcher = launcher;
        }

        private IWebApp _App;
        private IChromeLauncher _Launcher;
        private IWebContentHost _ContentHost = new WebContentHost();
        private IBrowser? _Browser;
        private IInteropSocket? _Socket;
        private object _InitLock = new object();

        public async Task LaunchAsync()
        {
            _ContentHost.StartHttpListener(_App.Content);
            var context = new WebAppContext(this);
            await _App.OnStartupAsync(context);
        }

        public async void Close()
        {
            _ContentHost.StopListening();
            if (_Socket != null)
                await _Socket.CloseAsync();
        }

        private async Task<IInteropSession> CreateNewSession(Uri url)
        {
            bool rootWindow = false;
            Monitor.Enter(_InitLock);
            if (_Socket == null)
            {
                rootWindow = true;
                _Browser = await _Launcher.LaunchAsync(url.ToString());
                var cdpSocket = new CDPSocket();
                await cdpSocket.ConnectAsync(_Browser.CDPTarget, CancellationToken.None);
                _Socket = new InteropSocket(cdpSocket);
            }
            Monitor.Enter(_InitLock);
            if (rootWindow)
            {
                var rootTarget = (await _Socket.GetTargetsAsync())
                    .Where(t => t.Type == DebugTargetType.Page)
                    .First();
                return await _Socket.OpenSessionAsync(rootTarget);
            } else
            {
                var newTarget = await _Socket.CreateTargetAsync(url);
                return await _Socket.OpenSessionAsync(newTarget);
            }
        }

        private class WebAppContext : IAppContext
        {
            private WebAppHost _Host;

            public WebAppContext(WebAppHost host)
            {
                _Host = host;
            }

            public async Task<IAppWindow> OpenWindow(string contentPath = "/")
            {
                var session = await _Host.CreateNewSession(_Host._ContentHost.GetContentUri(contentPath));
                return new AppWindow(_Host, session);
            }
        }

        private class AppWindow : IAppWindow
        {
            private WebAppHost _Host;
            private IInteropSession _Session;

            public AppWindow(WebAppHost host, IInteropSession session)
            {
                _Host = host;
                _Session = session;
            }

            public async Task CloseAsync()
            {
                await _Session.ClosePageAsync();
                await _Session.DetachAsync();
            }

            public async Task<IDOMNode> GetDocumentBodyAsync()
            {
                return await _Session.GetDOMDocumentAsync();
            }

            public async Task NavigateAsync(string contentPath)
            {
                await _Session.NavigatePageAsync(_Host._ContentHost.GetContentUri(contentPath));
            }
        }
    }
}
