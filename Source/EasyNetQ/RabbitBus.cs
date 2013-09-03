using System;
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
            Preconditions.CheckNotNull(serializeType, "serializeType");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(conventions, "conventions");

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

        public virtual void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
        {
            SubscribeAsync(subscriptionId, onMessage, x => { });
        }

        public virtual void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration<T>> configure)
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            var configuration = new SubscriptionConfiguration<T>();
            configure(configuration);

            var queueName = GetQueueName<T>(subscriptionId);
            var exchangeName = GetExchangeName<T>();

            var queue = advancedBus.QueueDeclare(queueName);
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);

            if(configuration.Topics.Count == 0)
            {
                advancedBus.Bind(exchange, queue, "#");
            }
            else
            {
                foreach (var topic in configuration.Topics)
                {
                    advancedBus.Bind(exchange, queue, topic);
                }
            }

            advancedBus.Consume<T>(queue, (message, messageRecievedInfo) => onMessage(message.Body));
        }

        private string GetExchangeName<T>()
        {
            return conventions.ExchangeNamingConvention(typeof(T));
        }

        private string GetQueueName<T>(string subscriptionId)
        {
            return conventions.QueueNamingConvention(typeof(T), subscriptionId);
        }

        public virtual void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            Preconditions.CheckNotNull(responder, "responder");

            Func<TRequest, Task<TResponse>> taskResponder =
                request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

            RespondAsync(taskResponder);
        }

        public virtual void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
        {
            Preconditions.CheckNotNull(responder, "responder");

            var routingKey = conventions.RpcRoutingKeyNamingConvention(typeof (TRequest));

            var exchange = advancedBus.ExchangeDeclare(conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);
            var queue = advancedBus.QueueDeclare(routingKey);
            advancedBus.Bind(exchange, queue, routingKey);

            advancedBus.Consume<TRequest>(queue, (requestMessage, messageRecievedInfo) =>
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