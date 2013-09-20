using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class RabbitPublishChannel : IPublishChannel
    {
        private readonly IAdvancedPublishChannel advancedPublishChannel;
        private readonly IAdvancedBus advancedBus;
        private readonly RabbitBus bus;
        private readonly IConventions conventions;

        public IAdvancedPublishChannel AdvancedPublishChannel
        {
            get { return advancedPublishChannel; }
        }

        public IAdvancedBus AdvancedBus
        {
            get { return advancedBus; }
        }

        public virtual IBus Bus
        {
            get { return bus; }
        }

        public RabbitPublishChannel(RabbitBus bus, Action<IChannelConfiguration> configure, IConventions conventions)
        {
            Preconditions.CheckNotNull(bus, "bus");
            Preconditions.CheckNotNull(configure, "configure");
            Preconditions.CheckNotNull(conventions, "conventions");

            this.bus = bus;
            this.conventions = conventions;
            advancedBus = bus.Advanced;
            advancedPublishChannel = advancedBus.OpenPublishChannel(configure);
        }

        public virtual void Publish<T>(T message) where T : class
        {
            Publish(message, x => {});
        }

        public virtual void Publish<T>(T message, Action<IPublishConfiguration<T>> configure) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(configure, "configure");

            var configuration = new PublishConfiguration<T>();
            configure(configuration);

            var exchangeName = bus.Conventions.ExchangeNamingConvention(typeof(T));
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);
            var easyNetQMessage = new Message<T>(message);

            // by default publish persistent messages
            easyNetQMessage.Properties.DeliveryMode = 2;

            var topic = configuration.Topics.Any()
                ? configuration.Topics[0]
                : bus.Conventions.TopicNamingConvention(typeof (T));

            advancedPublishChannel.Publish(exchange, topic, easyNetQMessage, MapConfiguration(configuration));
        }

        private Action<IAdvancedPublishConfiguration> MapConfiguration<T>(PublishConfiguration<T> configuration)
        {
            return x =>
            {
                if (configuration.SuccessCallback != null)
                {
                    x.OnSuccess(configuration.SuccessCallback);
                }
                if (configuration.FailureCallback != null)
                {
                    x.OnFailure(configuration.FailureCallback);
                }
            };
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
                passive:false, 
                durable:false, 
                exclusive:true, 
                autoDelete:true).SetAsSingleUse();

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

            advancedPublishChannel.Publish(exchange, routingKey, requestMessage, configuration => { });
        }

        public void Dispose()
        {
            advancedPublishChannel.Dispose();
        }
    }
}