﻿using Newtonsoft.Json.Linq;
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
        private LockedList<PendingSentRequest> _PendingRequests = new LockedList<PendingSentRequest>();
        private LockedList<IEventSubscription> _EventSubscriptions = new LockedList<IEventSubscription>();
        private object _SendSync = new object();
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

        public async Task RequestAsync(ICDPRequest message, string? sessionId = null)
        {
            var msgId = _Rand.Next();
            await SendMessageAsync(msgId, message, sessionId);
        }

        public async Task<TResult> RequestAsync<TResult>(ICDPRequest<TResult> message, string? sessionId)
        {
            var msgId = _Rand.Next();
            var pendingSentRequest = new PendingSentRequest(msgId);
            _PendingRequests.Acquire(l => l.Add(pendingSentRequest));
            await SendMessageAsync(msgId, message, sessionId);
            var resultObject = pendingSentRequest.WaitForResponse();
            return message.GetResultFromJson(resultObject);
        }

        public ICDPSubscription SubscribeEvent<TEventParams>(ICDPEvent<TEventParams> targetEvent, Action<TEventParams> handlerCallback, string? sessionId)
        {
            var subscription = new EventSubscription<TEventParams>(this, targetEvent, sessionId, handlerCallback);
            _EventSubscriptions.Acquire(subsriptions => subsriptions.Add(subscription));
            return subscription;
        }

        private async Task ListenAsync(CancellationToken cancelToken)
        {
            for (; ;)
            {
                var msgObject = await ReceiveRawAsync(cancelToken);
                cancelToken.ThrowIfCancellationRequested();
                //Console.Write($"Message received: {msgObject}");
                if (msgObject.ContainsKey("id"))
                {
                    var msgId = (int)msgObject["id"]!;
                    var resultObject = (JObject)msgObject["result"]!;
                    HandleRequestResponse(msgId, resultObject);
                } else if (msgObject.ContainsKey("method"))
                {
                    var sessionId = msgObject["sessionId"]?.ToString();
                    var methodName = msgObject["method"]!.ToString();
                    var paramsObject = (JObject)msgObject["params"]!;
                    HandleEvent(methodName, paramsObject, sessionId);
                }
            }
        }

        private void HandleRequestResponse(int id, JObject result)
        {
            PendingSentRequest? sentRequest = null;
            _PendingRequests.Acquire(pendingList =>
            {
                var matchingRequests = pendingList
                    .Select((r, i) => new KeyValuePair<int, PendingSentRequest>(i, r))
                    .Where(r => r.Value.Id == id);
                if (!matchingRequests.Any())
                    return;
                var pendingRequest = matchingRequests.First();
                pendingList.RemoveAt(pendingRequest.Key);
                sentRequest = pendingRequest.Value;
            });
            sentRequest?.Fulfill(result);
        }

        private void HandleEvent(string method, JObject paramsObject, string? sessionId)
        {
            _EventSubscriptions.Acquire(subscriptions =>
            {
                foreach (var subscription in subscriptions.Where(s => s.MethodName == method && (s.SessionId == null || s.SessionId == sessionId)))
                    Task.Run(() => subscription.Handle(paramsObject));
            });
        }

        private async Task SendMessageAsync(int id, ICDPRequest message, string? sessionId = null)
        {
            Monitor.Enter(_SendSync);
            await SendRawAsync(id, message, CancellationToken.None, sessionId);
            Monitor.Exit(_SendSync);
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

        private async Task SendRawAsync(int id, ICDPRequest message, CancellationToken cancelToken, string? sessionId = null)
        {
            var messageObject = new JObject();
            messageObject.Add("id", id);
            messageObject.Add("method", message.MethodName);
            messageObject.Add("params", message.GetJsonParams());
            if (sessionId != null)
                messageObject.Add("sessionId", sessionId);
            var messageJson = messageObject.ToString();
            var sendBuffer = Encoding.UTF8.GetBytes(messageJson);
            //Console.WriteLine($"Message sent: {messageObject}");
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
        }

        private class PendingSentRequest
        {
            public int Id { get; }

            public PendingSentRequest(int id)
            {
                Id = id;
            }

            private object _Sync = new object();
            private JObject? _Result;

            public void Fulfill(JObject requestResult)
            {
                _Result = requestResult;
                lock (_Sync)
                    Monitor.PulseAll(_Sync);
            }

            public JObject WaitForResponse()
            {
                if (_Result != null)
                    return _Result;
                lock (_Sync)
                {
                    if (_Result != null)
                        return _Result;
                    Monitor.Wait(_Sync);
                    return _Result!;
                }
            }
        }

        private interface IEventSubscription : ICDPSubscription
        {
            string MethodName { get; }
            string? SessionId { get; }
            void Handle(JObject jsonParams);
        }

        private class EventSubscription<TParams> : IEventSubscription
        {
            public ICDPEvent<TParams> Event { get; }
            public Action<TParams> Callback { get; }
            public string? SessionId { get; }
            public string MethodName => Event.MethodName;

            public EventSubscription(CDPSocket socket, ICDPEvent<TParams> subscribedEvent, string? sessionId, Action<TParams> callback)
            {
                _Socket = socket;
                Event = subscribedEvent;
                SessionId = sessionId;
                Callback = callback;
            }

            private CDPSocket _Socket;

            public void Handle(JObject jsonParams)
            {
                Event.Handle(jsonParams, Callback);
            }

            public void Unsubscribe()
            {
                _Socket._EventSubscriptions.Acquire(subscriptions => subscriptions.Remove(this));
            }
        }
    }
}
