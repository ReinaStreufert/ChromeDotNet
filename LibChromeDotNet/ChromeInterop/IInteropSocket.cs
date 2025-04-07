using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IInteropSocket : IDisposable
    {
        public Task CloseAsync();
        public Task<IEnumerable<IInteropTarget>> GetTargetsAsync();
        public Task<IInteropTarget> CreateTargetAsync(string url);
        public Task<IInteropTarget> CreateTargetAsync(Uri url);
        public Task<IInteropTarget> CreateTargetAsync(string url, int width, int height);
        public Task<IInteropTarget> CreateTargetAsync(Uri url, int width, int height);
        public Task EnableTargetDiscoveryAsync(Action<IInteropTarget> tarqetCreated, Action<string> targetDestroyed);
        public Task<IInteropSession> OpenSessionAsync(IInteropTarget target);
    }

    public interface IInteropTarget
    {
        public string Id { get; }
        public DebugTargetType Type { get; }
        public string Title { get; }
        public Uri NavigationUri { get; }
    }

    public enum DebugTargetType
    {
        Tab,
        Page,
        IFrame,
        Worker,
        Shared_Worker,
        Service_Worker,
        Worklet,
        Browser,
        WebView,
        Other,
        Auction_Worklet
    }
}
