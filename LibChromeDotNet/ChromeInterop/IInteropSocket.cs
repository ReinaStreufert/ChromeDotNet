﻿using System;
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
        public Task EnableTargetDiscoveryAsync(Action<IInteropTarget> tarqetCreated, Action<string> targetDestroyed);
        public Task<IInteropSession> OpenSessionAsync(IInteropTarget target, IEnumerable<IURIFetchHandler> handlers);
        public Task<IInteropSession> OpenSessionAsync(IInteropTarget target, params IURIFetchHandler[] handlers);
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
