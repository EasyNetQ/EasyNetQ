using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class RabbitBus : IBus
    {
        private readonly SerializeType serializeType;
        private readonly IEasyNetQLogger logger;
        private readonly IConventions conventions;
        private readonly IAdvancedBus advancedBus;

        public const string RpcExchange = "easy_net_q_rpc";

        public SerializeType SerializeType
        {
            get { return serializeType; }
        }

        public IEasyNetQLogger Logger
        {
            get { return logger; }
        }

        public IConventions Conventions
        {
            get { return conventions; }
        }

        public RabbitBus(
            SerializeType serializeType,
            IEasyNetQLogger logger,
            IConventions conventions,
            IAdvancedBus advancedBus)
        {
            if (serializeType == null)
            {
                throw new ArgumentNullException("serializeType");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (conventions == null)
            {
                throw new ArgumentNullException("conventions");
            }

            this.serializeType = serializeType;
            this.logger = logger;
            this.conventions = conventions;
            this.advancedBus = advancedBus;

            advancedBus.Connected += OnConnected;
            advancedBus.Disconnected += OnDisconnected;
        }

        public IPublishChannel OpenPublishChannel()
        {
            return OpenPublishChannel(x => { });
        }

        public IPublishChannel OpenPublishChannel(Action<IChannelConfiguration> configure)
        {
            return new RabbitPublishChannel(this, configure);
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            Subscribe(subscriptionId, onMessage, x => x.WithTopic("#"));
        }

        public void Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration<T>> configure)
        {
            SubscribeAsync(subscriptionId, msg =>
            {
                var tcs = new TaskCompletionSource<object>();
                try
                {
                    onMessage(msg);
                    tcs.SetResult(null);
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }
                return tcs.Task;
            },
            configure);
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
        {
            SubscribeAsync(subscriptionId, onMessage, x => x.WithTopic("#"));
        }

        public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration<T>> configure)
        {
            if(subscriptionId == null)
            {
                throw new ArgumentNullException("subscriptionId");
            }

            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            var configuration = new SubscriptionConfiguration<T>();
            configure(configuration);

            var queueName = GetQueueName<T>(subscriptionId);
            var exchangeName = GetExchangeName<T>();

            var queue = Queue.DeclareDurable(queueName, configuration.Arguments);
            var exchange = Exchange.DeclareTopic(exchangeName);
            queue.BindTo(exchange, configuration.Topics.ToArray());

            advancedBus.Subscribe<T>(queue, (message, messageRecievedInfo) => onMessage(message.Body));
        }

        private string GetExchangeName<T>()
        {
            return conventions.ExchangeNamingConvention(typeof(T));
        }

        private string GetQueueName<T>(string subscriptionId)
        {
            return conventions.QueueNamingConvention(typeof(T), subscriptionId);
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            Respond(responder, null);
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder, IDictionary<string, object> arguments)
        {
            if (responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            Func<TRequest, Task<TResponse>> taskResponder =
                request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

            RespondAsync(taskResponder, arguments);
        }

        public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        {
            RespondAsync(responder, null);
        }

        public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IDictionary<string, object> arguments)
        {
            if (responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            var requestTypeName = serializeType(typeof(TRequest));

            var exchange = Exchange.DeclareDirect(RpcExchange);
            var queue = Queue.DeclareDurable(requestTypeName, arguments);
            queue.BindTo(exchange, requestTypeName);

            advancedBus.Subscribe<TRequest>(queue, (requestMessage, messageRecievedInfo) =>
            {
                var tcs = new TaskCompletionSource<object>();

                responder(requestMessage.Body).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Console.WriteLine("task faulted");
                        if (task.Exception != null)
                        {
                            tcs.SetException(task.Exception);
                        }
                    }
                    else
                    {
                        // check we're connected
                        while (!advancedBus.IsConnected)
                        {
                            Thread.Sleep(100);
                        }

                        var responseMessage = new Message<TResponse>(task.Result);
                        responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;

                        using (var channel = advancedBus.OpenPublishChannel())
                        {
                            channel.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, responseMessage, configuration => {});
                        }
                        tcs.SetResult(null);
                    }
                });

                return tcs.Task;
            });
        }

        public event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null) Connected();
        }

        public event Action Disconnected;

        protected void OnDisconnected()
        {
            if (Disconnected != null) Disconnected();
        }

        public bool IsConnected
        {
            get { return advancedBus.IsConnected; }
        }

        public IAdvancedBus Advanced
        {
            get { return advancedBus; }
        }

        public void Dispose()
        {
            advancedBus.Dispose();
        }
    }
}