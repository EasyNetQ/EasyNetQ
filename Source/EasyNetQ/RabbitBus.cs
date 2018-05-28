using System;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using System.Linq;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    public class RabbitBus : IBus
    {
        private readonly IConventions conventions;
        private readonly IAdvancedBus advancedBus;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IRpc rpc;
        private readonly ISendReceive sendReceive;
        private readonly ConnectionConfiguration connectionConfiguration;

        public RabbitBus(
            IConventions conventions,
            IAdvancedBus advancedBus,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IRpc rpc,
            ISendReceive sendReceive,
            ConnectionConfiguration connectionConfiguration)
        {
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(rpc, "rpc");
            Preconditions.CheckNotNull(sendReceive, "sendReceive");
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");

            this.conventions = conventions;
            this.advancedBus = advancedBus;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.rpc = rpc;
            this.sendReceive = sendReceive;
            this.connectionConfiguration = connectionConfiguration;
        }

        public virtual void Publish<T>(T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            Publish(message, conventions.TopicNamingConvention(typeof(T)));
        }

        public virtual void Publish<T>(T message, string topic) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            Publish(message, c => c.WithTopic(topic));
        }

        public virtual void Publish<T>(T message, Action<IPublishConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(configure, "configure");

            var configuration = new PublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
            configure(configuration);

            var messageType = typeof(T);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType)
                }
            };
            if (configuration.Priority != null)
                easyNetQMessage.Properties.Priority = configuration.Priority.Value;
            if (configuration.Expires != null)
                easyNetQMessage.Properties.Expiration = configuration.Expires.ToString();

            var exchange = publishExchangeDeclareStrategy.DeclareExchange(messageType, ExchangeType.Topic);
            advancedBus.Publish(exchange, configuration.Topic, false, easyNetQMessage);
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

            return PublishAsync(message, c => c.WithTopic(topic));
        }

        public virtual async Task PublishAsync<T>(T message, Action<IPublishConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(configure, "configure");

            var configuration = new PublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
            configure(configuration);

            var messageType = typeof(T);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType)
                }
            };
            if (configuration.Priority != null)
                easyNetQMessage.Properties.Priority = configuration.Priority.Value;
            if (configuration.Expires != null)
                easyNetQMessage.Properties.Expiration = configuration.Expires.ToString();

            var exchange = await publishExchangeDeclareStrategy.DeclareExchangeAsync(messageType, ExchangeType.Topic).ConfigureAwait(false);
            await advancedBus.PublishAsync(exchange, configuration.Topic, false, easyNetQMessage).ConfigureAwait(false);
        }

        public virtual ISubscriptionResult Subscribe<T>(string subscriptionId, Action<T> onMessage) where T : class
        {
            return Subscribe(subscriptionId, onMessage, x => { });
        }

        public virtual ISubscriptionResult Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return SubscribeAsync<T>(subscriptionId, msg => TaskHelpers.ExecuteSynchronously(() => onMessage(msg)), configure);
        }

        public virtual ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage) where T : class
        {
            return SubscribeAsync(subscriptionId, onMessage, x => { });
        }

        public virtual ISubscriptionResult SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            var configuration = new SubscriptionConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration);

            var queueName = configuration.QueueName ?? conventions.QueueNamingConvention(typeof(T), subscriptionId);
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));

            var queue = advancedBus.QueueDeclare(
                queueName, 
                autoDelete: configuration.AutoDelete, 
                durable: configuration.Durable, 
                expires: configuration.Expires, 
                maxPriority: configuration.MaxPriority,
                maxLength: configuration.MaxLength,
                maxLengthBytes: configuration.MaxLengthBytes);
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);

            foreach (var topic in configuration.Topics.DefaultIfEmpty("#"))
            {
                advancedBus.Bind(exchange, queue, topic);
            }

            var consumerCancellation = advancedBus.Consume<T>(
                queue,
                (message, messageReceivedInfo) => onMessage(message.Body),
                x =>
                    {
                        x.WithPriority(configuration.Priority)
                         .WithPrefetchCount(configuration.PrefetchCount);
                        if (configuration.IsExclusive)
                        {
                            x.AsExclusive();
                        }
                    });
            return new SubscriptionResult(exchange, queue, consumerCancellation);
        }

        public virtual TResponse Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            return Request<TRequest, TResponse>(request, x => { });
        }

        public virtual TResponse Request<TRequest, TResponse>(TRequest request, Action<IRequestConfiguration> configure)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            return RequestAsync<TRequest, TResponse>(request, configure).GetAwaiter().GetResult();
        }

        public virtual Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            return rpc.Request<TRequest, TResponse>(request, x => { });
        }

        public virtual Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, Action<IRequestConfiguration> configure)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            return rpc.Request<TRequest, TResponse>(request, configure);
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

        public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder, Action<IResponderConfiguration> configure) where TRequest : class where TResponse : class
        {
            Func<TRequest, Task<TResponse>> taskResponder =
                request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

            return RespondAsync(taskResponder, configure);
        }

        public virtual IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            return RespondAsync(responder, c => { });
        }

        public IDisposable RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure) where TRequest : class where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");
            Preconditions.CheckNotNull(configure, "configure");

            return rpc.Respond(responder, configure);
        }

        public virtual void Send<T>(string queue, T message)
            where T : class
        {
            sendReceive.Send(queue, message);
        }

        public virtual Task SendAsync<T>(string queue, T message)
            where T : class
        {
            return sendReceive.SendAsync(queue, message);
        }

        public virtual IDisposable Receive<T>(string queue, Action<T> onMessage)
            where T : class
        {
            return sendReceive.Receive(queue, onMessage);
        }

        public virtual IDisposable Receive<T>(string queue, Action<T> onMessage, Action<IConsumerConfiguration> configure)
            where T : class
        {
            return sendReceive.Receive(queue, onMessage, configure);
        }

        public virtual IDisposable Receive<T>(string queue, Func<T, Task> onMessage)
            where T : class
        {
            return sendReceive.Receive(queue, onMessage);
        }

        public virtual IDisposable Receive<T>(string queue, Func<T, Task> onMessage, Action<IConsumerConfiguration> configure)
            where T : class
        {
            return sendReceive.Receive(queue, onMessage, configure);
        }

        public virtual IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers)
        {
            return sendReceive.Receive(queue, addHandlers);
        }

        public virtual IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers, Action<IConsumerConfiguration> configure)
        {
            return sendReceive.Receive(queue, addHandlers, configure);
        }

        public virtual bool IsConnected => advancedBus.IsConnected;

        public virtual IAdvancedBus Advanced => advancedBus;

        public virtual void Dispose()
        {
            advancedBus.Dispose();
        }
    }
}