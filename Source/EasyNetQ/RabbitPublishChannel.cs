using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    public class RabbitPublishChannel : IPublishChannel, IRawByteBus
    {
        private readonly IModel channel;
        private readonly RabbitBus bus;

        public RabbitPublishChannel(RabbitBus bus)
        {
            this.bus = bus;

            channel = bus.Connection.CreateModel();
        }

        public void Publish<T>(T message)
        {
            Publish(bus.Conventions.TopicNamingConvention(typeof(T)), message);
        }

        public void Publish<T>(string topic, T message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            var typeName = bus.SerializeType(typeof(T));
            var exchangeName = bus.Conventions.ExchangeNamingConvention(typeof(T));
            var messageBody = bus.Serializer.MessageToBytes(message);

            RawPublish(exchangeName, topic, typeName, messageBody);
        }

        public void RawPublish(string exchangeName, string topic, string typeName, byte[] messageBody)
        {
            if (disposed)
            {
                throw new EasyNetQException("PublishChannel is already disposed");
            }

            if (!bus.Connection.IsConnected)
            {
                throw new EasyNetQException("Publish failed. No rabbit server connected.");
            }

            try
            {
                DeclarePublishExchange(exchangeName);

                var defaultProperties = channel.CreateBasicProperties();
                defaultProperties.SetPersistent(false);
                defaultProperties.Type = typeName;
                defaultProperties.CorrelationId = bus.GetCorrelationId();

                channel.BasicPublish(
                    exchangeName, // exchange
                    topic, // routingKey 
                    defaultProperties, // basicProperties
                    messageBody); // body

                bus.Logger.DebugWrite("Published {0}, CorrelationId {1}", exchangeName, defaultProperties.CorrelationId);
            }
            catch (OperationInterruptedException exception)
            {
                throw new EasyNetQException("Publish Failed: '{0}'", exception.Message);
            }
            catch (System.IO.IOException exception)
            {
                throw new EasyNetQException("Publish Failed: '{0}'", exception.Message);
            }
        }

        private void DeclarePublishExchange(string exchangeName)
        {
            // no need to declare on every publish
            if (bus.PublishExchanges.Add(exchangeName))
            {
                channel.ExchangeDeclare(
                    exchangeName,               // exchange
                    ExchangeType.Topic,    // type
                    true);                  // durable

                bus.Logger.DebugWrite("Declared publish exchange");
            }
        }

        public void FuturePublish<T>(DateTime timeToRespond, T message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (!bus.Connection.IsConnected)
            {
                throw new EasyNetQException("FuturePublish failed. No rabbit server connected.");
            }

            var typeName = bus.SerializeType(typeof(T));
            var messageBody = bus.Serializer.MessageToBytes(message);

            Publish(new ScheduleMe
            {
                WakeTime = timeToRespond,
                BindingKey = typeName,
                InnerMessage = messageBody
            });
        }

        public void RawPublish(string exchangeName, byte[] messageBody)
        {
            RawPublish(exchangeName, "", messageBody);
        }

        public void RawPublish(string typeName, string topic, byte[] messageBody)
        {
            var exchangeName = typeName;
            RawPublish(exchangeName, topic, typeName, messageBody);
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
            if (disposed)
            {
                throw new EasyNetQException("PublishChannel is already disposed");
            }
            if (!bus.Connection.IsConnected)
            {
                throw new EasyNetQException("Publish failed. No rabbit server connected.");
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
            var responseChannel = bus.Connection.CreateModel();
            bus.ModelList.Add(responseChannel);

            // respond queue is transient, only exists for the lifetime of the service.
            var respondQueue = responseChannel.QueueDeclare(
                returnQueueName,
                false,              // durable
                true,               // exclusive
                true,               // autoDelete
                null                // arguments
                );

            var consumer = bus.ConsumerFactory.CreateConsumer(responseChannel,
                (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                {
                    bus.CheckMessageType<TResponse>(properties);
                    var response = bus.Serializer.BytesToMessage<TResponse>(body);

                    var tcs = new TaskCompletionSource<object>();

                    try
                    {
                        onResponse(response);
                        tcs.SetResult(null);
                    }
                    catch (Exception exception)
                    {
                        tcs.SetException(exception);
                    }
                    return tcs.Task;
                });

            responseChannel.BasicConsume(
                respondQueue,           // queue
                RabbitBus.NoAck,                  // noAck 
                consumer.ConsumerTag,   // consumerTag
                consumer);              // consumer
        }

        private void RequestPublish<TRequest>(TRequest request, string returnQueueName)
        {
            var requestTypeName = bus.SerializeType(typeof(TRequest));

            // declare the exchange, binding and queue here. No need to set the mandatory flag, the recieving queue
            // will already have been declared, so in the case of no responder being present, message will collect
            // there.
            DeclareRequestResponseStructure(channel, requestTypeName);

            // tell the consumer to respond to the transient respondQueue
            var requestProperties = channel.CreateBasicProperties();
            requestProperties.ReplyTo = returnQueueName;
            requestProperties.Type = requestTypeName;

            var requestBody = bus.Serializer.MessageToBytes(request);
            channel.BasicPublish(
                RabbitBus.RpcExchange,            // exchange 
                requestTypeName,        // routingKey 
                requestProperties,      // basicProperties 
                requestBody);           // body
        }

        private void DeclareRequestResponseStructure(IModel channel, string requestTypeName)
        {
            if (bus.RequestExchanges.Add(requestTypeName))
            {
                bus.Logger.DebugWrite("Declaring Request/Response structure for request: {0}", requestTypeName);

                channel.ExchangeDeclare(
                    RabbitBus.RpcExchange, // exchange 
                    ExchangeType.Direct, // type 
                    false, // autoDelete 
                    true, // durable 
                    null); // arguments

                channel.QueueDeclare(
                    requestTypeName, // queue 
                    true, // durable 
                    false, // exclusive 
                    false, // autoDelete 
                    null); // arguments

                channel.QueueBind(
                    requestTypeName, // queue
                    RabbitBus.RpcExchange, // exchange 
                    requestTypeName); // routingKey
            }
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            channel.Abort();
            channel.Dispose();
            disposed = true;
        }
    }
}