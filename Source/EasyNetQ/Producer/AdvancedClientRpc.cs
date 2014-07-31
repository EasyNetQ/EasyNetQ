using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    interface IRpcHeaderKeys
    {
        string IsFaultedKey { get; }
        string ExceptionMessageKey { get; }
    }

    class RpcHeaderKeys : IRpcHeaderKeys
    {
        public string IsFaultedKey { get { return "IsFaulted"; } }
        public string ExceptionMessageKey { get { return "ExceptionMessage"; } }
    }

    class AdvancedClientRpc : IAdvancedClientRpc
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly IConnectionConfiguration _configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;
        private readonly ConcurrentDictionary<string, ResponseAction> responseActions = new ConcurrentDictionary<string, ResponseAction>();
        
        private readonly TimeSpan disablePeriodicSignaling = TimeSpan.FromMilliseconds(-1);
        

        public AdvancedClientRpc(IAdvancedBus advancedBus, IConnectionConfiguration configuration, IEventBus eventBus, IRpcHeaderKeys rpcHeaderKeys)
        {
            _advancedBus = advancedBus;
            _configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;

            eventBus.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated);
        }
        
        public Task<SerializedMessage> Request(IExchange requestExchange, string requestRoutingKey, bool mandatory, bool immediate, Func<string> responseQueueNameFactory, SerializedMessage request)
        {
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();

            var tcs = new TaskCompletionSource<SerializedMessage>();
            var timer = new Timer(state =>
                {
                    ((Timer)state).Dispose();
                    tcs.TrySetException(new TimeoutException(
                                            string.Format("Request timed out. CorrelationId: {0}", correlationId.ToString())));
                });

            timer.Change(TimeSpan.FromSeconds(_configuration.Timeout), disablePeriodicSignaling);

            RegisterErrorHandling(correlationId, timer, tcs);

            var responseQueueName = responseQueueNameFactory();
            
            SubscribeToResponse(responseQueueName);
            RequestPublish(requestExchange, request, requestRoutingKey, responseQueueName, correlationId);

            return tcs.Task;
        }

        private void RequestPublish(IExchange requestExchange, SerializedMessage request, string requestRoutingKey, string responseQueueName, Guid correlationId)
        {
            request.Properties.ReplyTo = responseQueueName;
            request.Properties.CorrelationId = correlationId.ToString();
            request.Properties.Expiration = TimeSpan.FromSeconds(_configuration.Timeout).TotalMilliseconds.ToString();

            _advancedBus.Publish(requestExchange, requestRoutingKey, false, false, request.Properties, request.Body);
        }

        private void RegisterErrorHandling(Guid correlationId, Timer timer, TaskCompletionSource<SerializedMessage> tcs)
        {
            responseActions.TryAdd(correlationId.ToString(), new ResponseAction
                {
                    OnSuccess = msg =>
                        {
                            timer.Dispose();

                            var isFaulted = false;
                            var exceptionMessage = "The exception message has not been specified.";
                            if (msg.Properties.HeadersPresent)
                            {
                                if (msg.Properties.Headers.ContainsKey(_rpcHeaderKeys.IsFaultedKey))
                                {
                                    isFaulted = Convert.ToBoolean(msg.Properties.Headers[_rpcHeaderKeys.IsFaultedKey]);
                                }
                                if (msg.Properties.Headers.ContainsKey(_rpcHeaderKeys.ExceptionMessageKey))
                                {
                                    exceptionMessage = Encoding.UTF8.GetString((byte[])msg.Properties.Headers[_rpcHeaderKeys.ExceptionMessageKey]);
                                }
                            }

                            if (isFaulted)
                            {
                                tcs.TrySetException(new EasyNetQResponderException(exceptionMessage));
                            }
                            else
                            {
                                tcs.TrySetResult(msg);
                            }
                        },
                    OnFailure = () =>
                        {
                            timer.Dispose();
                            tcs.TrySetException(new EasyNetQException(
                                                    "Connection lost while request was in-flight. CorrelationId: {0}", correlationId.ToString()));
                        }
                });
        }

        private void SubscribeToResponse(string responseQueueName)
        {
            var queue = _advancedBus.QueueDeclare(
                responseQueueName,
                passive: false,
                durable: false,
                expires: (int) TimeSpan.FromSeconds(_configuration.Timeout).TotalMilliseconds,
                exclusive: true,
                autoDelete: true);

            //the response is published to the default exchange with the queue name as routingkey. So no need to bind to exchange
            var disposeWhenSet = new DisposeWhenSet();
            var consumingDisposer = _advancedBus.Consume(queue, (bytes, msgProp, messageReceivedInfo) => Task.Factory.StartNew(() =>
            {
                ResponseAction responseAction;
                if (responseActions.TryRemove(msgProp.CorrelationId, out responseAction))
                {
                    responseAction.OnSuccess(new SerializedMessage(msgProp, bytes));
                }
                disposeWhenSet.DisposeObject();
            }), c => c.WithPrefetchCount(1));
            disposeWhenSet.Disposable = consumingDisposer;
        }

        private void OnConnectionCreated(ConnectionCreatedEvent @event)
        {
            var copyOfResponseActions = responseActions.Values;
            responseActions.Clear();

            // retry in-flight requests.
            foreach (var responseAction in copyOfResponseActions)
            {
                responseAction.OnFailure();
            }
        }

        private class ResponseAction
        {
            public Action<SerializedMessage> OnSuccess { get; set; }
            public Action OnFailure { get; set; }
        }

    }
}