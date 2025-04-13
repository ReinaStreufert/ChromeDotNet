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
    public class WebAppContext : IAppContext
    {
        private IChromeLauncher _Launcher;
        private IWebContentHost _ContentHost;

        public WebAppContext(IChromeLauncher launcher, IWebContentHost contentHost)
        {
            _Launcher = launcher;
            _ContentHost = contentHost;
        }

        private object _Sync = new object();
        private Task<IInteropSocket>? _Socket;
        private List<AppWindow> _OpenedWindows = new List<AppWindow>();
        private List<AppWindow> _LoadingWindows = new List<AppWindow>();
        private AppWindow? _FocusedWindow;
        private bool _IsExited = false;

        public async Task ExitAsync()
        {
            lock (_Sync)
            {
                if (_IsExited || _Socket == null)
                    return;
                _IsExited = true;
            }
            foreach (var window in _OpenedWindows)
                await window.CloseAsync();
            var socket = await _Socket;
            socket.Dispose();
            _ContentHost.Stop();
            
        }

        public async Task<IAppWindow> OpenWindowAsync(string contentPath = "/")
        {
            var contentProvider = _ContentHost.CreateContentProvider();
            var window = new AppWindow(this, contentPath, contentProvider);
            lock (_Sync)
            {
                if (_IsExited)
                    throw new InvalidOperationException("The app context has exited");
                _LoadingWindows.Add(window);
            }
            CreateWindow(contentProvider.GetContentUri(contentPath));
            Interlocked.Exchange(ref _FocusedWindow, window);
            await Task.Run(window.WaitForSession);
            return window;
        }

        private void OnWindowClosed(AppWindow window)
        {
            lock (_Sync)
            {
                _OpenedWindows.Remove(window);
                if (_OpenedWindows.Count == 0)
                    _ContentHost.Stop();
            }
        }

        private void CreateWindow(Uri uri)
        {
            lock (_Sync)
            {
                if (_Socket == null)
                {
                    _Socket = LaunchBrowserAsync(uri);
                    return;
                }
            }
            if (_FocusedWindow == null)
                throw new InvalidOperationException("Focused AppWindow should be set by the first caller to OpenWindowAsync, which is the only caller of this method.");
            _FocusedWindow.WaitForSession();
            _ = _FocusedWindow.CreatePopupAsync(uri);
        }

        private async Task<IInteropSocket> LaunchBrowserAsync(Uri rootWindowUri)
        {
            var browser = await _Launcher.LaunchAsync(rootWindowUri.ToString());
            var cdp = new CDPSocket();
            await cdp.ConnectAsync(browser.CDPTarget, CancellationToken.None);
            var socket = new InteropSocket(cdp);
            await socket.EnableTargetDiscoveryAsync(async t => await OnTargetDiscoveredAsync(socket, t));
            return socket;
        }

        private async Task OnTargetDiscoveredAsync(IInteropSocket socket, IInteropTarget target)
        {
            var uri = target.NavigationUri;
            if (target.Type == DebugTargetType.Page)
            {
                AppWindow? window;
                lock (_Sync)
                {
                    window = _LoadingWindows
                        .Where(w => w.LocalHostUri == uri)
                        .FirstOrDefault();
                    if (window != null)
                    {
                        _LoadingWindows.Remove(window);
                        _OpenedWindows.Add(window);
                    }
                }
                if (window == null)
                    return;
                var session = await socket.OpenSessionAsync(target);
                window.SetSession(session);
                //await socket.EnableTargetDiscoveryAsync(async t => await OnTargetDiscoveredAsync(socket, t));
            }
        }

        private class AppWindow : IAppWindow
        {
            public string DocumentLocation => _CurrentContentPath;
            public Uri LocalHostUri => _ContentProvider.GetContentUri(_CurrentContentPath);
            public event Action? ClosedByUser;

            public AppWindow(WebAppContext context, string currentContentPath, IWebContentProvider contentProvider)
            {
                _Context = context;
                _CurrentContentPath = currentContentPath;
                _ContentProvider = contentProvider;
            }

            private WebAppContext _Context;
            private bool _IsClosed = false;
            private string _CurrentContentPath;
            private IWebContentProvider _ContentProvider;
            private IInteropSession? _Session;
            private object _SessionSync = new object();

            public void WaitForSession()
            {
                lock (_SessionSync)
                {
                    for (; ;)
                    {
                        if (_Session != null)
                            return;
                        Monitor.Wait(_SessionSync);
                    }
                }
            }

            public async void SetSession(IInteropSession session)
            {
                session.Detached += () =>
                {
                    _IsClosed = true;
                    ClosedByUser?.Invoke();
                    _Context.OnWindowClosed(this);
                };
                await using (var wAddEventListener = (IJSFunction)await session.EvaluateExpressionAsync("window.addEventListener"))
                await using (var jsCallback = await session.AddJSBindingAsync((JObject o) => Interlocked.Exchange(ref _Context._FocusedWindow, this)))
                {
                    await wAddEventListener.CallAsync(
                        IJSValue.FromString("focus"),
                        jsCallback);
                }
                var loadTaskSource = new TaskCompletionSource();
                session.PageLoaded += loadTaskSource.SetResult;
                var docReadyExpr = "(function(url) { return document.URL.toLowerCase() == url.toLowerCase() && document.readyState != \"loading\" })";
                JSValue<bool> docReady;
                await using (var docReadyFunc = (IJSFunction)await session.EvaluateExpressionAsync(docReadyExpr))
                    docReady = (JSValue<bool>)await docReadyFunc.CallAsync(IJSValue.FromString(LocalHostUri.ToString()));
                if (!docReady.Value)
                    await loadTaskSource.Task;
                session.PageLoaded -= loadTaskSource.SetResult;
                lock (_SessionSync)
                {
                    if (_Session != null)
                        throw new InvalidOperationException("if this is thrown something is deeply wrong...");
                    _Session = session;
                    Monitor.PulseAll(_SessionSync);
                }
            }

            public async Task CreatePopupAsync(Uri url)
            {
                if (_IsClosed)
                    throw new InvalidOperationException("Window is closed");
                if (_Session == null)
                    throw new InvalidOperationException("call WaitForSession");
                var jsWindowOpenExpr = "(function(url,name){ window.open(url, name, \"popup=true,noopener=true,noreferrer=true\"); })"; // random popup names ensure a new window is always generated.
                await using (var jsWindowOpenFunc = (IJSFunction)await _Session.EvaluateExpressionAsync(jsWindowOpenExpr))
                    await jsWindowOpenFunc.CallAsync(IJSValue.FromString(url.ToString()), IJSValue.FromString(Identifier.New()));
            }

            public async Task CloseAsync()
            {
                if (_IsClosed)
                    return;
                _IsClosed = true;
                if (_Session == null)
                    throw new InvalidOperationException("call WaitForSession");
                await _Session.ClosePageAsync();
                _Context.OnWindowClosed(this);
            }

            public async Task<IDOMNode> GetDocumentBodyAsync()
            {
                if (_IsClosed)
                    throw new InvalidOperationException("Window is closed");
                if (_Session == null)
                    throw new InvalidOperationException("call WaitForSession");
                return await _Session.GetDOMDocumentAsync();
            }

            public async Task NavigateAsync(string contentPath)
            {
                if (_IsClosed)
                    throw new InvalidOperationException("Window is closed");
                if (_Session == null)
                    throw new InvalidOperationException("call WaitForSession");
                var navTask = _Session.NavigatePageAsync(_ContentProvider.GetContentUri(contentPath));
                Interlocked.Exchange(ref _CurrentContentPath, contentPath);
                await navTask;
            }
        }
    }
}
