﻿using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using EasyNetQ.Rpc.FreshQueue;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc.ReuseQueue
{
    class ReuseQueueAdvancedClientRpc : IAdvancedClientRpc
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly IConnectionConfiguration _configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;
        private readonly string _responseQueueName;
        private readonly TimeSpan _disablePeriodicSignaling = TimeSpan.FromMilliseconds(-1);
        
        private readonly ConcurrentDictionary<string, ResponseAction> _responseActions = new ConcurrentDictionary<string, ResponseAction>();
        private IDisposable _consumer;
        private bool _firstTime = true;

        public ReuseQueueAdvancedClientRpc(
            IAdvancedBus advancedBus, 
            IConnectionConfiguration configuration, 
            IRpcHeaderKeys rpcHeaderKeys, 
            IEventBus eventBus, 
            string responseQueueName)
        {
            _advancedBus = advancedBus;
            _configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
            _responseQueueName = responseQueueName;
            eventBus.Subscribe<ConnectionCreatedEvent>(_ => OnConnectionCreated());
            eventBus.Subscribe<ConnectionDisconnectedEvent>(_ => OnConnectionDisconnected());
        }

        public Task<SerializedMessage> RequestAsync(IExchange requestExchange, string requestRoutingKey, bool mandatory, bool immediate, TimeSpan timeout, SerializedMessage request)
        {
            Preconditions.CheckNotNull(request, "request");
            Preconditions.CheckNotNull(requestExchange, "requestExchange");
            Preconditions.CheckNotNull(requestRoutingKey, "requestRoutingKey");
            if (_firstTime)
            {
                CreateQueueAndConsume();
                _firstTime = false;
            }

            var correlationId = Guid.NewGuid();

            var tcs = new TaskCompletionSource<SerializedMessage>();

            var timer = new Timer(state => tcs.TrySetException(new TimeoutException(string.Format("Request timed out. CorrelationId: {0}", correlationId.ToString()))));
            timer.Change(TimeSpan.FromSeconds(_configuration.Timeout), _disablePeriodicSignaling);

            _responseActions.TryAdd(correlationId.ToString(), new ResponseAction
                {
                    OnSuccess = sm => RpcHelpers.ExtractExceptionFromHeadersAndPropagateToTaskCompletionSource(_rpcHeaderKeys,sm, tcs),
                    ConnectionLost = () => tcs.TrySetException(new EasyNetQException("Connection lost while request was in-flight. CorrelationId: {0}", correlationId.ToString()))
                });

            RpcHelpers.PublishRequest(_advancedBus, requestExchange, request, requestRoutingKey, _responseQueueName, correlationId, timeout);

            return tcs.Task.ContinueWithSideEffect(timer.Dispose);
        }

        private void OnConnectionDisconnected()
        {
            if (_consumer != null)
                _consumer.Dispose();
        }

        private void OnConnectionCreated()
        {
            var copyOfResponseActions = _responseActions.Values;
            _responseActions.Clear();

            // retry in-flight requests.
            foreach (var responseAction in copyOfResponseActions)
            {
                responseAction.ConnectionLost();
            }

            CreateQueueAndConsume();
        }

        private void CreateQueueAndConsume()
        {
            //TODO The queue is exclusive, which gives it a transient consumer. (Wait what ??)
            //This is bad bad bad. It is relying on being a non-reconsuming (transient) consumer, due to the fact that it is exclusive. MUST BE FIXED asap
            var queue = _advancedBus.QueueDeclare(
                _responseQueueName,
                passive: false,
                durable: false,
                exclusive: true,
                autoDelete: true);

            _consumer = _advancedBus.Consume(queue, (bytes, properties, info) => Task.Factory.StartNew(() => 
                {
                    var msg = new SerializedMessage(properties, bytes);
                    ResponseAction responseAction;
                    if (_responseActions.TryRemove(msg.Properties.CorrelationId, out responseAction))
                    {
                        responseAction.OnSuccess(msg);
                    }
                }));
        }


        private class ResponseAction
        {
            public Action<SerializedMessage> OnSuccess { get; set; }
            public Action ConnectionLost { get; set; }
        }
    }
}