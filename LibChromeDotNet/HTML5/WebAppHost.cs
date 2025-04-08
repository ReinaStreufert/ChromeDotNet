using LibChromeDotNet.CDP;
using LibChromeDotNet.ChromeApplication;
using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
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
        private Task<IInteropSocket>? _InitTask;
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
            if (_InitTask != null)
            {
                var socket = await _InitTask;
                await socket.CloseAsync();
            }
        }

        private async Task<IInteropSocket> InitializeChromeAsync(string initialUrl)
        {
            var browser = await _Launcher.LaunchAsync(initialUrl);
            var cdp = new CDPSocket();
            await cdp.ConnectAsync(browser.CDPTarget, CancellationToken.None);
            return new InteropSocket(cdp);
        }

        private async Task<IInteropSession> CreateNewSession(Uri url)
        {
            bool rootWindow = false;
            lock (_InitLock)
            {
                if (_InitTask == null)
                {
                    rootWindow = true;
                    _InitTask = InitializeChromeAsync(url.ToString());
                }
            }
            var socket = await _InitTask;
            if (rootWindow)
            {
                var rootTarget = (await socket.GetTargetsAsync())
                    .Where(t => t.Type == DebugTargetType.Page)
                    .First();
                return await socket.OpenSessionAsync(rootTarget);
            }
            else
            {
                var newTarget = await socket.CreateTargetAsync(url);
                return await socket.OpenSessionAsync(newTarget);
            }
        }

        private class WebAppContext : IAppContext
        {
            private WebAppHost _Host;

            public WebAppContext(WebAppHost host)
            {
                _Host = host;
            }

            public async Task<IAppWindow> OpenWindowAsync(string contentPath = "/")
            {
                var session = await _Host.CreateNewSession(_Host._ContentHost.GetContentUri(contentPath));
                var loadTaskSource = new TaskCompletionSource();
                session.PageLoaded += loadTaskSource.SetResult;
                var readyState = (await session.EvaluateExpressionAsync("document.readyState")).ToString();
                if (readyState == "loading")
                    await loadTaskSource.Task;
                session.PageLoaded -= loadTaskSource.SetResult;
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
