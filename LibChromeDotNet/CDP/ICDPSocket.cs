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
        public Task RequestAsync(ICDPRequest message);
        public Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> message);
        public void SubscribeEvent<TEventParams>(ICDPEvent<TEventParams> targetEvent, Action<TEventParams> handlerCallback);
    }
}
