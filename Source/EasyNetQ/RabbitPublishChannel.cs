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

            // rather than setting up a subscription on each call of Request, we cache a single
            // subscription keyed on the hashcode of the onResponse action. This has a couple of
            // consequences:
            //  1.  Closures don't work as expected since the closed over variable is always the first
            //      one that was called.
            //  2.  Worries about the uniqueness of MethodInfo.GetHashCode. Looking at the CLR source
            //      it seems that it's not overriden so it is the same as Object.GetHashCode(). This
            //      is unique for an instance in an app-domain, so it _should_ be OK for this usage.
            var uniqueResponseQueueName = "EasyNetQ_return_" + Guid.NewGuid().ToString();
            if (bus.ResponseQueueNameCache.TryAdd(onResponse.Method.GetHashCode(), uniqueResponseQueueName))
            {
                bus.Logger.DebugWrite("Setting up return subscription for req/resp {0} {1}",
                    typeof(TRequest).Name,
                    typeof(TResponse).Name);

                SubscribeToResponse(onResponse, uniqueResponseQueueName);
            }

            var returnQueueName = bus.ResponseQueueNameCache[onResponse.Method.GetHashCode()];

            RequestPublish(request, returnQueueName);
        }

        private void SubscribeToResponse<TResponse>(Action<TResponse> onResponse, string returnQueueName)
        {
            var queue = Queue.DeclareTransient(returnQueueName);

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