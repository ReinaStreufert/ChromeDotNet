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
            Interlocked.Exchange(ref _FocusedWindow, window);
            lock (_Sync)
            {
                if (_IsExited)
                    throw new InvalidOperationException("The app context has exited");
                _OpenedWindows.Add(window);
            }
            CreateWindow(contentProvider.GetContentUri(contentPath));
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
            await socket.EnableAutoAttachAsync(async s => await OnSessionAttachAsync(s));
            return socket;
        }

        private async Task OnSessionAttachAsync(IInteropSession session)
        {
            var uri = session.SessionTarget.NavigationUri;
            lock (_Sync)
            {
                foreach (var window in _OpenedWindows)
                {
                    if (window.LocalHostUri == uri)
                    {
                        window.SetSession(session);
                        break;
                    }
                }
            }
            // when session does not correspond to an AppWindow:
            await session.DetachAsync();
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

            public void SetSession(IInteropSession session)
            {
                lock (_SessionSync)
                {
                    if (_Session != null)
                        throw new InvalidOperationException("if this is thrown something is deeply wrong...");
                    _Session = session;
                    Monitor.PulseAll(_SessionSync);
                }
                session.Detached += () =>
                {
                    _IsClosed = true;
                    ClosedByUser?.Invoke();
                    _Context.OnWindowClosed(this);
                };

            }

            public Task CreatePopupAsync(Uri uri)
            {

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
