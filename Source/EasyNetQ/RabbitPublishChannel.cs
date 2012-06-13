using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class RabbitPublishChannel : IPublishChannel
    {
        private readonly IAdvancedPublishChannel advancedPublishChannel;
        private readonly IAdvancedBus advancedBus;
        private readonly RabbitBus bus;

        public IAdvancedPublishChannel AdvancedPublishChannel
        {
            get { return advancedPublishChannel; }
        }

        public IAdvancedBus AdvancedBus
        {
            get { return advancedBus; }
        }

        public IBus Bus
        {
            get { return bus; }
        }

        public RabbitPublishChannel(RabbitBus bus)
        {
            this.bus = bus;
            advancedBus = bus.Advanced;
            advancedPublishChannel = advancedBus.OpenPublishChannel();
        }

        public void Publish<T>(T message)
        {
            Publish(bus.Conventions.TopicNamingConvention(typeof(T)), message);
        }

        public void Publish<T>(string topic, T message)
        {
            if(topic == null)
            {
                throw new ArgumentNullException("topic");
            }
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            var exchangeName = bus.Conventions.ExchangeNamingConvention(typeof(T));
            var exchange = Exchange.DeclareTopic(exchangeName);
            advancedPublishChannel.Publish(exchange, topic, new Message<T>(message));
        }

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse)
        {
            if (onResponse == null)
            {
                throw new ArgumentNullException("onResponse");
            }
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var returnQueueName = SubscribeToResponse(onResponse);

            RequestPublish(request, returnQueueName);
        }

        private string SubscribeToResponse<TResponse>(Action<TResponse> onResponse)
        {
            var queue = Queue.DeclareTransient("easynetq.response." + Guid.NewGuid().ToString());

            advancedBus.Subscribe<TResponse>(queue, (message, messageRecievedInfo) =>
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

        private void RequestPublish<TRequest>(TRequest request, string returnQueueName)
        {
            var requestTypeName = bus.SerializeType(typeof(TRequest));
            var exchange = Exchange.DeclareDirect(RabbitBus.RpcExchange);

            var requestMessage = new Message<TRequest>(request);
            requestMessage.Properties.ReplyTo = returnQueueName;

            advancedPublishChannel.Publish(exchange, requestTypeName, requestMessage);
        }

        public void Dispose()
        {
            advancedPublishChannel.Dispose();
        }
    }
}