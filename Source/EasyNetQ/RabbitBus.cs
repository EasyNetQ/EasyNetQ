using System;
using System.Collections.Concurrent;
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
        private readonly IRpc rpc;
        
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
            IRpc rpc)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(rpc, "rpc");

            this.logger = logger;
            this.conventions = conventions;
            this.advancedBus = advancedBus;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.rpc = rpc;

            advancedBus.Connected += OnConnected;
            advancedBus.Disconnected += OnDisconnected;
        }

        public void Publish<T>(T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            PublishAsync(message).Wait();
        }

        public void Publish<T>(T message, string topic) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            PublishAsync(message, topic).Wait();
        }

        public Task PublishAsync<T>(T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            return PublishAsync(message, conventions.TopicNamingConvention(typeof(T)));
        }

        public Task PublishAsync<T>(T message, string topic) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var exchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);
            var easyNetQMessage = new Message<T>(message);

            // by default publish persistent messages
            easyNetQMessage.Properties.DeliveryMode = 2;

            return advancedBus.PublishAsync(exchange, topic, false, false, easyNetQMessage);
        }

        public virtual IDisposable Subscribe<T>(string subscriptionId, Action<T> onMessage) where T : class
        {
            return Subscribe(subscriptionId, onMessage, x => { });
        }

        public virtual IDisposable Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration<T>> configure) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return SubscribeAsync(subscriptionId, msg =>
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

        public virtual IDisposable SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration<T>> configure) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            var configuration = new SubscriptionConfiguration<T>();
            configure(configuration);

            var queueName = conventions.QueueNamingConvention(typeof(T), subscriptionId);
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));

            var queue = advancedBus.QueueDeclare(queueName);
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);

            foreach (var topic in configuration.Topics.AtLeastOneWithDefault("#"))
            {
                advancedBus.Bind(exchange, queue, topic);
            }

            return advancedBus.Consume<T>(queue, (message, messageRecievedInfo) => onMessage(message.Body));
        }

        public TResponse Request<TRequest, TResponse>(TRequest request) where TRequest : class where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var task = RequestAsync<TRequest, TResponse>(request);
            task.Wait();
            return task.Result;
        }

        public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            return rpc.Request<TRequest, TResponse>(request);
        }

        public virtual void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder) 
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");

            Func<TRequest, Task<TResponse>> taskResponder =
                request => Task<TResponse>.Factory.StartNew(_ => responder(request), null);

            RespondAsync(taskResponder);
        }

        public virtual void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder) 
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");
            
            rpc.Respond(responder);
        }

        public void Send<T>(string queue, T message)
            where T : class
        {
            advancedBus.Publish(Exchange.GetDefault(), queue, false, false, new Message<T>(message));
        }

        private readonly ConcurrentDictionary<string, Tuple<IHandlerRegistration, IDisposable>> handlerCollections =
            new ConcurrentDictionary<string, Tuple<IHandlerRegistration, IDisposable>>(); 

        public IDisposable Receive<T>(string queue, Action<T> onMessage)
            where T : class
        {
            return Receive<T>(queue, message => TaskHelpers.ExecuteSynchronously(() => onMessage(message)));
        }

        public IDisposable Receive<T>(string queue, Func<T, Task> onMessage)
            where T : class
        {
            IDisposable disposable = null;
            handlerCollections.AddOrUpdate(
                queue,
                key =>
                    {
                        var declaredQueue = advancedBus.QueueDeclare(queue);
                        IHandlerRegistration handlerRegistration = null;
                        disposable = advancedBus.Consume(declaredQueue, registration =>
                            {
                                registration.Add<T>((message, info) => onMessage(message.Body));
                                handlerRegistration = registration;
                            });
                        return new Tuple<IHandlerRegistration, IDisposable>(handlerRegistration, disposable);
                    },
                (key, value) =>
                    {
                        var registration = value.Item1;
                        disposable = value.Item2;
                        registration.Add<T>((message, info) => onMessage(message.Body));
                        return value;
                    });
            return disposable;
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