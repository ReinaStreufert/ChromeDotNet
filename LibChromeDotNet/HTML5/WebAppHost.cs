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
            private Task<IInteropSession>? _InitTask;
            private object _InitLock = new object();

            public async Task<IAppWindow> OpenWindowAsync(string contentPath = "/")
            {
                lock (_Sync)
                {
                    if (_ExitLock)
                        ThrowExited();
                }
                var contentProvider = _Host._ContentHost.CreateContentProvider();
                var contentUri = contentProvider.GetContentUri(contentPath);
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
                var result = new AppWindow(_Host, session, contentProvider);
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
                bool isRootWindow = false;
                lock (_InitLock)
                {
                    if (_InitTask == null)
                    {
                        isRootWindow = true;
                        _InitTask = InitializeChromeAsync(url);
                    }
                }
                var rootSession = await _InitTask;
                if (isRootWindow)
                    return rootSession;
                var jsWindowOpenExpr = "(function(url){ window.open(url, null, {newWindow: true}); })";
                await using (var jsWindowOpenFunc = (IJSFunction)await rootSession.EvaluateExpressionAsync(jsWindowOpenExpr))
                    await jsWindowOpenFunc.CallAsync(IJSValue.FromString(url.ToString()));
                var socket = rootSession.Socket;
                var newTarget = (await socket.GetTargetsAsync())
                    .Where(t => t.Type == DebugTargetType.Page && t.NavigationUri == url)
                    .First();
                return await socket.OpenSessionAsync(newTarget);
            }

            private async Task<IInteropSession> InitializeChromeAsync(Uri initialUrl)
            {
                var browser = await _Host._Launcher.LaunchAsync(initialUrl.ToString());
                var cdp = new CDPSocket();
                await cdp.ConnectAsync(browser.CDPTarget, CancellationToken.None);
                var sock = new InteropSocket(cdp);
                sock.Detached += _Host._ContentHost.Stop;
                var rootTarget = (await sock.GetTargetsAsync())
                    .Where(t => t.Type == DebugTargetType.Page && t.NavigationUri == initialUrl)
                    .First();
                return await sock.OpenSessionAsync(rootTarget);
            }
        }

        private class AppWindow : IAppWindow
        {
            private WebAppHost _Host;
            private IWebContentProvider _ContentProvider;
            private IInteropSession _Session;

            public AppWindow(WebAppHost host, IInteropSession session, IWebContentProvider contentProvider)
            {
                _Host = host;
                _Session = session;
                _ContentProvider = contentProvider;
            }

            public async Task CloseAsync()
            {
                await _Session.ClosePageAsync();
                await _Session.DetachAsync();
                _ContentProvider.Dispose();
            }

            public async Task<IDOMNode> GetDocumentBodyAsync()
            {
                return await _Session.GetDOMDocumentAsync();
            }

            public async Task NavigateAsync(string contentPath)
            {
                await _Session.NavigatePageAsync(_ContentProvider.GetContentUri(contentPath));
            }
        }
    }
}
