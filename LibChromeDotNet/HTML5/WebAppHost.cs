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

        public async Task LaunchAppAsync()
        {
            var context = new WebAppContext(this);
            var listenTask = _ContentHost.ListenAsync(_App.Content);
            await _App.OnStartupAsync(context);
            await listenTask;
        }

        private class WebAppContext : IAppContext
        {
            public WebAppContext(WebAppHost host)
            {
                _Host = host;
            }

            private WebAppHost _Host;
            private List<IAppWindow> _OpenWindows = new List<IAppWindow>();
            private object _Sync = new object();
            private bool _ExitLock = false;
            private Task<IInteropSocket>? _InitTask;
            private object _InitLock = new object();

            public async Task<IAppWindow> OpenWindowAsync(string contentPath = "/")
            {
                lock (_Sync)
                {
                    if (_ExitLock)
                        ThrowExited();
                }
                var contentUri = _Host._ContentHost.GetContentUri(contentPath);
                var session = await CreateNewSession(contentUri);
                var loadTaskSource = new TaskCompletionSource();
                session.PageLoaded += loadTaskSource.SetResult;
                var docReadyExpr = "(function(url) { return document.URL.toLowerCase() == url.toLowerCase() && document.readyState != \"loading\" })";
                JSValue<bool> docReady;
                await using (var docReadyFunc = (IJSFunction)await session.EvaluateExpressionAsync(docReadyExpr))
                    docReady = (JSValue<bool>)await docReadyFunc.CallAsync(IJSValue.FromString(contentUri.ToString()));
                if (!docReady.Value)
                    await loadTaskSource.Task;
                session.PageLoaded -= loadTaskSource.SetResult;
                var result = new AppWindow(_Host, session);
                lock (_Sync)
                {
                    if (_ExitLock)
                    {
                        _ = result.CloseAsync();
                        ThrowExited();
                    }
                    _OpenWindows.Add(result);
                }
                return result;
            }

            public void Exit()
            {
                _Host._ContentHost.Stop();
                lock (_Sync)
                {
                    foreach (var window in _OpenWindows)
                        _ = window.CloseAsync();
                    _ExitLock = true;
                }
            }

            private void ThrowExited() => throw new InvalidOperationException("The context is invalid because the app has exited");

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

            private async Task<IInteropSocket> InitializeChromeAsync(string initialUrl)
            {
                var browser = await _Host._Launcher.LaunchAsync(initialUrl);
                var cdp = new CDPSocket();
                await cdp.ConnectAsync(browser.CDPTarget, CancellationToken.None);
                var sock = new InteropSocket(cdp);
                sock.Detached += _Host._ContentHost.Stop;
                return sock;
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
