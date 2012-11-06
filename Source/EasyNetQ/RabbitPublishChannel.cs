using System;
using System.Collections.Generic;
using System.Linq;
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

        public RabbitPublishChannel(RabbitBus bus, Action<IChannelConfiguration> configure)
        {
            this.bus = bus;
            advancedBus = bus.Advanced;
            advancedPublishChannel = advancedBus.OpenPublishChannel(configure);
        }

        public void Publish<T>(T message)
        {
            Publish(message, x => {});
        }

        public void Publish<T>(T message, Action<IPublishConfiguration<T>> configure)
        {
            if(message == null)
            {
                throw new ArgumentNullException("message");
            }
            if(configure == null)
            {
                throw new ArgumentNullException("configure");
            }

            var configuration = new PublishConfiguration<T>();
            configure(configuration);

            var exchangeName = bus.Conventions.ExchangeNamingConvention(typeof(T));
            var exchange = Exchange.DeclareTopic(exchangeName);
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
        {
            Request(request, onResponse, null);
        }

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse, IDictionary<string, object> arguments)
        {
            if (onResponse == null)
            {
                throw new ArgumentNullException("onResponse");
            }
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var returnQueueName = SubscribeToResponse(onResponse, arguments);
            RequestPublish(request, returnQueueName);
        }


        private string SubscribeToResponse<TResponse>(Action<TResponse> onResponse, IDictionary<string, object> arguments)
        {
            var queue = Queue.DeclareTransient("easynetq.response." + Guid.NewGuid().ToString(), arguments).SetAsSingleUse();

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

            advancedPublishChannel.Publish(exchange, requestTypeName, requestMessage, configuration => {});
        }

        public void Dispose()
        {
            advancedPublishChannel.Dispose();
        }
    }
}