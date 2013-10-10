using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class RabbitBus : IBus
    {
        private readonly SerializeType serializeType;
        private readonly IEasyNetQLogger logger;
        private readonly IConventions conventions;
        private readonly IAdvancedBus advancedBus;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        
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
            IAdvancedBus advancedBus, 
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy)
        {
            Preconditions.CheckNotNull(serializeType, "serializeType");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");

            this.serializeType = serializeType;
            this.logger = logger;
            this.conventions = conventions;
            this.advancedBus = advancedBus;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;

            advancedBus.Connected += OnConnected;
            advancedBus.Disconnected += OnDisconnected;
        }

        public void Publish<T>(T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            Publish(message, conventions.TopicNamingConvention(typeof(T)));
        }

        public void Publish<T>(T message, string topic) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var exchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName);
            var easyNetQMessage = new Message<T>(message);

            // by default publish persistent messages
            easyNetQMessage.Properties.DeliveryMode = 2;

            advancedBus.Publish(exchange, topic, false, false, easyNetQMessage);
        }

        public virtual void Subscribe<T>(string subscriptionId, Action<T> onMessage) where T : class
        {
            Subscribe(subscriptionId, onMessage, x => { });
        }

        public virtual void Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration<T>> configure) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

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

        public virtual void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage) where T : class
        {
            SubscribeAsync(subscriptionId, onMessage, x => { });
        }

        public virtual void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration<T>> configure) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

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

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(onResponse, "onResponse");
            Preconditions.CheckNotNull(request, "request");

            var returnQueueName = SubscribeToResponse(onResponse);
            RequestPublish(request, returnQueueName);
        }

        public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var taskCompletionSource = new TaskCompletionSource<TResponse>();

            Request<TRequest, TResponse>(request, response => taskCompletionSource.TrySetResult(response));

            return taskCompletionSource.Task;
        }

        public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken token)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var taskCompletionSource = new TaskCompletionSource<TResponse>();
            token.Register(() => taskCompletionSource.TrySetCanceled());

            Request<TRequest, TResponse>(request, response => taskCompletionSource.TrySetResult(response));

            return taskCompletionSource.Task;
        }

        private string SubscribeToResponse<TResponse>(Action<TResponse> onResponse)
            where TResponse : class
        {
            var queue = advancedBus.QueueDeclare(
                conventions.RpcReturnQueueNamingConvention(),
                passive: false,
                durable: false,
                exclusive: true,
                autoDelete: true).SetAsSingleUse();

            advancedBus.Consume<TResponse>(queue, (message, messageRecievedInfo) =>
            {
                var tcs = new TaskCompletionSource<object>();

                try
                {
                    onResponse(message.Body);
                    tcs.SetResult(null);
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }
                return tcs.Task;
            });

            return queue.Name;
        }

        private void RequestPublish<TRequest>(TRequest request, string returnQueueName) where TRequest : class
        {
            var routingKey = conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));
            var exchange = advancedBus.ExchangeDeclare(conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);

            var requestMessage = new Message<TRequest>(request);
            requestMessage.Properties.ReplyTo = returnQueueName;

            advancedBus.Publish(exchange, routingKey, false, false, requestMessage);
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
                        if (task.Exception != null)
                        {
                            tcs.SetException(task.Exception);
                        }
                    }
                    else
                    {
                        var responseMessage = new Message<TResponse>(task.Result);
                        responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;

                        advancedBus.Publish(new Exchange(""), requestMessage.Properties.ReplyTo, false, false, responseMessage);
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