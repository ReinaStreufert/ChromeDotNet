using LibChromeDotNet.CDP;
using LibChromeDotNet.CDP.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class InteropSocket : IInteropSocket
    {
        public InteropSocket(ICDPSocket cdpSocket)
        {
            _CDP = cdpSocket;
        }

        private ICDPSocket _CDP;
        private string? _LastBrowserContextId;

        public void Dispose() => _CDP.Dispose();
        public Task CloseAsync() => _CDP.CloseAsync();

        public async Task EnableTargetDiscoveryAsync(Action<IInteropTarget> tarqetCreated, Action<string> targetDestroyed)
        {
            _CDP.SubscribeEvent(Target.OnTargetCreated, targetInfo => tarqetCreated(targetInfo));
            _CDP.SubscribeEvent(Target.OnTargetDestroyed, targetDestroyed);
            await _CDP.RequestAsync(Target.SetDiscoverTargets(true));
        }

        public async Task<IEnumerable<IInteropTarget>> GetTargetsAsync()
        {
            return (await _CDP.RequestAsync(Target.GetTargets()))
                .Cast<IInteropTarget>();
        }

        public async Task<IInteropSession> OpenSessionAsync(IInteropTarget target)
        {
            var sessionId = await _CDP.RequestAsync(Target.AttachToTarget(target.Id));
            Interlocked.Exchange(ref _LastBrowserContextId, target.BrowserContextId);
            var session = new InteropSession(this, target, _CDP, sessionId);
            _ = session.RequestAsync(Page.Enable);
            _ = session.RequestAsync(Runtime.Enable);
            return session;
        }

        public async Task ActivateTargetAsync(IInteropTarget target)
        {
            await _CDP.RequestAsync(Target.ActivateTarget(target.Id));
        }
    }
}
