using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    public interface ICDPSocket : IDisposable
    {
        public Task CloseAsync();
        public Task ConnectAsync(ICDPRemoteHost host, CancellationToken cancelToken);
        public Task RequestAsync(ICDPRequest message, string? sessionId = null);
        public Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> message, string? sessionId = null);
        public ICDPSubscription SubscribeEvent<TEventParams>(ICDPEvent<TEventParams> targetEvent, Action<TEventParams> handlerCallback, string? sessionId = null);
    }

    public interface ICDPSubscription
    {
        public void Unsubscribe();
    }
}
