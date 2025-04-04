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
        private ICDPSocket _CDP;

        public InteropSocket(ICDPSocket cdpSocket)
        {
            _CDP = cdpSocket;
        }

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

        public Task<IInteropSession> OpenSessionAsync(IInteropTarget target, IEnumerable<IURIFetchHandler> handlers)
        {
            throw new NotImplementedException();
        }

        public Task<IInteropSession> OpenSessionAsync(IInteropTarget target, params IURIFetchHandler[] handlers)
        {
            throw new NotImplementedException();
        }
    }
}
