using LibChromeDotNet.CDP;
using LibChromeDotNet.ChromeApplication;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.JS;
using Newtonsoft.Json.Linq;
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
            private List<AppWindow> _OpenWindows = new List<AppWindow>();
            private IInteropSession? _FocusedSession;

            private object _Sync = new object();
            private bool _IsExited = false;
            private Task<IInteropSession>? _InitTask;
            private object _InitLock = new object();

            public async Task<IAppWindow> OpenWindowAsync(string contentPath = "/")
            {
                lock (_Sync)
                {
                    if (_IsExited)
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
                AppWindow result;
                lock (_Sync)
                {
                    result = new AppWindow(this, session, contentProvider, _OpenWindows.Count == 0);
                    if (_IsExited)
                    {
                        _ = result.CloseAsync();
                        ThrowExited();
                    }
                    _OpenWindows.Add(result);
                }
                Interlocked.Exchange(ref _FocusedSession, session);
                await using (var windowEventListener = (IJSFunction)await session.EvaluateExpressionAsync("window.addEventListener"))
                await using (var jsCallback = await session.AddJSBindingAsync((JObject o) => Interlocked.Exchange(ref _FocusedSession, session)))
                {
                    await windowEventListener.CallAsync(
                        IJSValue.FromString("focus"),
                        jsCallback);
                }
                return result;
            }

            public void Exit()
            {
                lock (_Sync)
                {
                    foreach (var window in _OpenWindows)
                        _ = window.CloseAsync();
                    _IsExited = true;
                }
                _Host._ContentHost.Stop();
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
                if (isRootWindow)
                    return await _InitTask;
                IInteropSession focused;
                focused = _FocusedSession ?? await _InitTask;
                var socket = focused.Socket;
                var jsWindowOpenExpr = "(function(url,name){ window.open(url, name, \"popup=true,noopener=true,noreferrer=true\"); })"; // random popup names ensure a new window is always generated.
                await using (var jsWindowOpenFunc = (IJSFunction)await focused.EvaluateExpressionAsync(jsWindowOpenExpr))
                    await jsWindowOpenFunc.CallAsync(IJSValue.FromString(url.ToString()), IJSValue.FromString(Identifier.New()));
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
                var rootTarget = (await sock.GetTargetsAsync())
                    .Where(t => t.Type == DebugTargetType.Page && t.NavigationUri == initialUrl)
                    .First();
                var rootSession = await sock.OpenSessionAsync(rootTarget);
                rootSession.Detached += Exit; // the first launched window acts as the root window for the application, the whole app dies with it.
                return rootSession;
            }
        }

        private class AppWindow : IAppWindow
        {
            private WebAppContext _Context;
            private IWebContentProvider _ContentProvider;
            private IInteropSession _Session;
            private bool _IsRoot;

            public AppWindow(WebAppContext context, IInteropSession session, IWebContentProvider contentProvider, bool isRoot)
            {
                _Context = context;
                _Session = session;
                _ContentProvider = contentProvider;
                _IsRoot = isRoot;
                session.Detached += () => ClosedByUser?.Invoke();
            }

            public event Action? ClosedByUser;

            public async Task CloseAsync()
            {
                _ContentProvider.Dispose();
                if (_IsRoot)
                    _Context.Exit();
                else
                    await _Session.ClosePageAsync();
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
