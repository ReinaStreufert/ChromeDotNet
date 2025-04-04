using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    public class CDPSocket : ICDPSocket
    {
        private const int ReceiveBufferLen = ushort.MaxValue;

        private ClientWebSocket _WebSocket = new ClientWebSocket();

        public CDPSocket()
        {
            _ReceiveBuffer = new byte[ReceiveBufferLen];
        }

        private byte[] _ReceiveBuffer;
        private Random _Rand = new Random();
        private LockedList<QueuedRequest> _QueuedRequests = new LockedList<QueuedRequest>();
        private LockedList<PendingSentRequest> _PendingRequests = new LockedList<PendingSentRequest>();
        private LockedList<IEventSubscription> _EventSubscriptions = new LockedList<IEventSubscription>();
        private object _SendSweepLock = new object();
        private Task<DateTime>? _LastSweepTask;
        private CancellationTokenSource _ListenerCancelSource = new CancellationTokenSource();

        public async Task CloseAsync()
        {
            _ListenerCancelSource.Cancel();
            await _WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        public async Task ConnectAsync(ICDPRemoteHost host, CancellationToken cancelToken)
        {
            await _WebSocket.ConnectAsync(host.RemoteWSHost, cancelToken);
            _ = ListenAsync(_ListenerCancelSource.Token);
        }

        public async Task RequestAsync(ICDPRequest message)
        {
            var msgId = _Rand.Next();
            await SendMessageAsync(msgId, message);
        }

        public async Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> message)
        {
            var msgId = _Rand.Next();
            var pendingSentRequest = new PendingSentRequest(msgId);
            _PendingRequests.Acquire(l => l.Add(pendingSentRequest));
            await SendMessageAsync(msgId, message);
            var resultObject = await pendingSentRequest.GetResponseAsync();
            return message.GetResultFromJson(resultObject);
        }

        public void SubscribeEvent<TEventParams>(ICDPEvent<TEventParams> targetEvent, Action<TEventParams> handlerCallback)
        {
            _EventSubscriptions.Acquire(subsriptions =>
            {
                subsriptions.Add(new EventSubscription<TEventParams>(targetEvent, handlerCallback));
            });
        }

        private async Task<DateTime> SweepMessageQueue(DateTime minValidTime)
        {
            for (; ;)
            {
                var sweepTime = await GetOrStartMessageQueueSweep();
                if (sweepTime >= minValidTime)
                    return sweepTime;
            }
        }

        private Task<DateTime> GetOrStartMessageQueueSweep()
        {
            lock (_SendSweepLock)
            {
                if (_LastSweepTask == null)
                    _LastSweepTask = SendQueuedMessagesAsync();
                return _LastSweepTask;
            }
        }

        private async Task<DateTime> SendQueuedMessagesAsync()
        {
            var snapshot = _QueuedRequests.AcquireSnapshot(true);
            foreach (var message in snapshot.Items)
                await SendRawAsync(message.Id, message.Request, CancellationToken.None);
            return snapshot.Time;
        }

        private async Task ListenAsync(CancellationToken cancelToken)
        {
            for (; ;)
            {
                var msgObject = await ReceiveRawAsync(cancelToken);
                cancelToken.ThrowIfCancellationRequested();
                if (msgObject.ContainsKey("id"))
                {
                    var msgId = (int)msgObject["id"]!;
                    var resultObject = (JObject)msgObject["result"]!;
                    HandleRequestResponse(msgId, resultObject);
                } else if (msgObject.ContainsKey("method"))
                {
                    var methodName = msgObject["method"]!.ToString();
                    var paramsObject = (JObject)msgObject["params"]!;
                    HandleEvent(methodName, paramsObject);
                }
            }
        }

        private void HandleRequestResponse(int id, JObject result)
        {
            _PendingRequests.Acquire(pendingList =>
            {
                var matchingRequests = pendingList
                    .Select((r, i) => new KeyValuePair<int, PendingSentRequest>(i, r))
                    .Where(r => r.Value.Id == id);
                if (!matchingRequests.Any())
                    return;
                var pendingRequest = matchingRequests.First();
                pendingList.RemoveAt(pendingRequest.Key);
                pendingRequest.Value.Fulfill(result);
            });
        }

        private void HandleEvent(string method, JObject paramsObject)
        {
            _EventSubscriptions.Acquire(subscriptions =>
            {
                foreach (var subscription in subscriptions.Where(s => s.MethodName == method))
                    subscription.Handle(paramsObject);
            });
        }

        private async Task SendMessageAsync(int id, ICDPRequest message)
        {
            var time = _QueuedRequests.Acquire(l =>
            {
                l.Add(new QueuedRequest(id, message));
            });
            await SweepMessageQueue(time);
        }

        private async Task<JObject> ReceiveRawAsync(CancellationToken cancelToken)
        {
            StringBuilder? sb = null;
            for (; ;)
            {
                var receiveResult = await _WebSocket.ReceiveAsync(_ReceiveBuffer, cancelToken);
                cancelToken.ThrowIfCancellationRequested();
                var messagePart = Encoding.UTF8.GetString(_ReceiveBuffer, 0, receiveResult.Count);
                if (sb == null && receiveResult.EndOfMessage)
                    return JObject.Parse(messagePart);
                if (sb == null)
                    sb = new StringBuilder();
                sb.Append(messagePart);
                if (receiveResult.EndOfMessage)
                    return JObject.Parse(sb.ToString());
            }
        }

        private async Task SendRawAsync(int id, ICDPRequest message, CancellationToken cancelToken)
        {
            var messageObject = new JObject();
            messageObject.Add("id", id);
            messageObject.Add("method", message.MethodName);
            messageObject.Add("params", message.GetJsonParams());
            var messageJson = messageObject.ToString();
            var sendBuffer = Encoding.UTF8.GetBytes(messageJson);
            await _WebSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, cancelToken);
        }

        public void Dispose()
        {
            _WebSocket.Dispose();
        }

        private class QueuedRequest
        {
            public int Id { get; }
            public ICDPRequest Request { get; }

            public QueuedRequest(int id, ICDPRequest request)
            {
                Id = id;
                Request = request;
            }

            private TaskCompletionSource _TaskSource = new TaskCompletionSource();

            public void MarkSent() => _TaskSource.SetResult();
            public async Task WaitUntilSentAsync() => await _TaskSource.Task;
        }

        private class PendingSentRequest
        {
            public int Id { get; }

            public PendingSentRequest(int id)
            {
                Id = id;
            }

            private TaskCompletionSource<JObject> _TaskSource = new TaskCompletionSource<JObject>();

            public void Fulfill(JObject requestResult)
            {
                _TaskSource.SetResult(requestResult);
            }

            public async Task<JObject> GetResponseAsync()
            {
                return await _TaskSource.Task;
            }
        }

        private interface IEventSubscription
        {
            string MethodName { get; }
            void Handle(JObject jsonParams);
        }

        private class EventSubscription<TParams> : IEventSubscription
        {
            public ICDPEvent<TParams> Event { get; }
            public Action<TParams> Callback { get; }
            public string MethodName => Event.MethodName;

            public EventSubscription(ICDPEvent<TParams> subscribedEvent, Action<TParams> callback)
            {
                Event = subscribedEvent;
                Callback = callback;
            }

            public void Handle(JObject jsonParams)
            {
                Event.Handle(jsonParams, Callback);
            }
        }
    }
}
