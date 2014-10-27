using System;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class RabbitBus : IBus
    {
        private readonly IEasyNetQLogger logger;
        private readonly IConventions conventions;
        private readonly IAdvancedBus advancedBus;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IRpc rpc;
        private readonly ISendReceive sendReceive;
        private readonly ConnectionConfiguration connectionConfiguration;

        public IEasyNetQLogger Logger
        {
            get { return logger; }
        }

        public IConventions Conventions
        {
            get { return conventions; }
        }

        public RabbitBus(
            IEasyNetQLogger logger,
            IConventions conventions,
            IAdvancedBus advancedBus,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IRpc rpc,
            ISendReceive sendReceive,
            ConnectionConfiguration connectionConfiguration)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(rpc, "rpc");
            Preconditions.CheckNotNull(sendReceive, "sendReceive");
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");

            this.logger = logger;
            this.conventions = conventions;
            this.advancedBus = advancedBus;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.rpc = rpc;
            this.sendReceive = sendReceive;
            this.connectionConfiguration = connectionConfiguration;

            advancedBus.Connected += OnConnected;
            advancedBus.Disconnected += OnDisconnected;
        }

        public virtual void Publish<T>(T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            PublishAsync(message).Wait();
        }

        public virtual void Publish<T>(T message, string topic) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            PublishAsync(message, topic).Wait();
        }

        public virtual Task PublishAsync<T>(T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            return PublishAsync(message, conventions.TopicNamingConvention(typeof(T)));
        }

        public virtual Task PublishAsync<T>(T message, string topic) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");
            var messageType = typeof (T);
            return publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, messageType, ExchangeType.Topic).Then(exchange =>
                {
                    var easyNetQMessage = new Message<T>(message) { Properties = { DeliveryMode = (byte)(messageDeliveryModeStrategy.IsPersistent(messageType) ? 2 : 1) } };
                    return advancedBus.PublishAsync(exchange, topic, false, false, easyNetQMessage); 
                });
        }

        public virtual IDisposable Subscribe<T>(string subscriptionId, Action<T> onMessage) where T : class
        {
            return Subscribe(subscriptionId, onMessage, x => { });
        }

        public virtual IDisposable Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return SubscribeAsync<T>(subscriptionId, msg =>
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

        public virtual IDisposable SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage) where T : class
        {
            return SubscribeAsync(subscriptionId, onMessage, x => { });
        }

        public virtual IDisposable SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            var configuration = new SubscriptionConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration);

            var queueName = conventions.QueueNamingConvention(typeof(T), subscriptionId);
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));

            var queue = advancedBus.QueueDeclare(queueName, autoDelete: configuration.AutoDelete, expires: configuration.Expires);
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);

            foreach (var topic in configuration.Topics.AtLeastOneWithDefault("#"))
            {
                advancedBus.Bind(exchange, queue, topic);
            }

            return advancedBus.Consume<T>(
                queue,
                (message, messageReceivedInfo) => onMessage(message.Body),
                x => x.WithPriority(configuration.Priority)
                      .WithCancelOnHaFailover(configuration.CancelOnHaFailover)
                      .WithPrefetchCount(configuration.PrefetchCount));
        }

        public virtual TResponse Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var task = RequestAsync<TRequest, TResponse>(request);
            task.Wait();
            return task.Result;
        }

        public virtual Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            return rpc.Request<TRequest, TResponse>(request);
        }

        public virtual IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");

            Func<TRequest, Task<TResponse>> taskResponder =
                request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

            return RespondAsync(taskResponder);
        }

        public virtual IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");

            return rpc.Respond(responder);
        }

        public virtual void Send<T>(string queue, T message)
            where T : class
        {
            sendReceive.Send(queue, message);
        }

        public virtual IDisposable Receive<T>(string queue, Action<T> onMessage)
            where T : class
        {
            return sendReceive.Receive(queue, onMessage);
        }

        public virtual IDisposable Receive<T>(string queue, Func<T, Task> onMessage)
            where T : class
        {
            return sendReceive.Receive(queue, onMessage);
        }

        public virtual IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers)
        {
            return sendReceive.Receive(queue, addHandlers);
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