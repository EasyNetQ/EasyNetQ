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

        public virtual IPublishChannel OpenPublishChannel()
        {
            return OpenPublishChannel(x => { });
        }

        public virtual IPublishChannel OpenPublishChannel(Action<IChannelConfiguration> configure)
        {
            return new RabbitPublishChannel(this, configure, conventions);
        }

        public virtual void Subscribe<T>(string subscriptionId, Action<T> onMessage)
        {
            Subscribe(subscriptionId, onMessage, x => { });
        }

        public virtual void Subscribe(string subscriptionId, Type messageType, Action<object> onMessage)
        {
            Subscribe(subscriptionId, messageType, onMessage, x => { });
        }

        public virtual void Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration<T>> configure)
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

        public virtual void Subscribe(string subscriptionId, Type messageType, Action<object> onMessage, Action<ISubscriptionConfiguration> configure)
        {
            SubscribeAsync(subscriptionId, messageType, msg =>
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

        public virtual void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
        {
            SubscribeAsync(subscriptionId, onMessage, x => { });
        }

        public virtual void SubscribeAsync(string sunscriptionId, Type messageType, Func<object, Task> onMessage)
        {
            SubscribeAsync(sunscriptionId, messageType, onMessage, x => { });
        }

        public virtual void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration<T>> configure)
        {
            if(subscriptionId == null)
            {
                throw new ArgumentNullException("subscriptionId");
            }

            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            Func<object, Task> typedOnMessage = msg => onMessage((T) msg);
            Action<ISubscriptionConfiguration> typedConfigure = config => configure(new SubscriptionConfigurationWrapper<T>(config));

            SubscribeAsync(subscriptionId, typeof(T), typedOnMessage, typedConfigure);
        }

        public virtual void SubscribeAsync(string subscriptionId, Type messageType, Func<object, Task> onMessage, Action<ISubscriptionConfiguration> configure)
        {
            if (subscriptionId == null)
            {
                throw new ArgumentNullException("subscriptionId");
            }

            if (onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            var configuration = new SubscriptionConfiguration();
            configure(configuration);

            var queueName = GetQueueName(subscriptionId, messageType);
            var exchangeName = GetExchangeName(messageType);

            var queue = Queue.DeclareDurable(queueName, configuration.Arguments);
            var exchange = Exchange.DeclareTopic(exchangeName);

            var topics = configuration.Topics.ToArray();

            if (topics.Length == 0)
            {
                topics = new[] { "#" };
            }

            queue.BindTo(exchange, topics);

            advancedBus.Subscribe(messageType, queue, (message, messageRecievedInfo) => onMessage(message.Body));
        }

        private string GetExchangeName(Type messageType)
        {
            return conventions.ExchangeNamingConvention(messageType);
        }

        private string GetQueueName(string subscriptionId, Type messageType)
        {
            return conventions.QueueNamingConvention(messageType, subscriptionId);
        }

        public virtual void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            Respond(responder, null);
        }

        public virtual void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder, IDictionary<string, object> arguments)
        {
            if (responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            Func<TRequest, Task<TResponse>> taskResponder =
                request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

            RespondAsync(taskResponder, arguments);
        }

        public virtual void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        {
            RespondAsync(responder, null);
        }

        public virtual void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IDictionary<string, object> arguments)
        {
            if (responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            var requestTypeName = serializeType(typeof(TRequest));

            var exchange = Exchange.DeclareDirect(conventions.RpcExchangeNamingConvention());
            var queue = Queue.DeclareDurable(requestTypeName, arguments);
            queue.BindTo(exchange, requestTypeName);

            advancedBus.Subscribe<TRequest>(queue, (requestMessage, messageRecievedInfo) =>
            {
                var tcs = new TaskCompletionSource<object>();

                responder(requestMessage.GetBody()).ContinueWith(task =>
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

        public virtual event Action Connected;

        protected void OnConnected()
        {
            if (Connected != null) Connected();
        }

        public virtual event Action Disconnected;

        protected void OnDisconnected()
        {
            if (Disconnected != null) Disconnected();
        }

        public virtual bool IsConnected
        {
            get { return advancedBus.IsConnected; }
        }

        public virtual IAdvancedBus Advanced
        {
            get { return advancedBus; }
        }

        public virtual void Dispose()
        {
            advancedBus.Dispose();
        }
    }
}