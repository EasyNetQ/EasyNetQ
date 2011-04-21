using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class RabbitBus : IBus
    {
        private readonly SerializeType serializeType;
        private readonly SubsriberNameFromDelegate subscriberNameFromDelegate;
        private readonly ISerializer serializer;
        private readonly IConnection connection;

        private const string rpcExchange = "rpc";

        public RabbitBus(
            SerializeType serializeType, 
            SubsriberNameFromDelegate subscriberNameFromDelegate,
            ISerializer serializer,
            IConnection connection)
        {
            if(serializeType == null)
            {
                throw new ArgumentNullException("serializeType");
            }
            if(subscriberNameFromDelegate == null)
            {
                throw new ArgumentNullException("subscriberNameFromDelegate");
            }
            if(serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }
            if(connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            this.serializeType = serializeType;
            this.subscriberNameFromDelegate = subscriberNameFromDelegate;
            this.serializer = serializer;
            this.connection = connection;
        }

        public void Publish<T>(T message)
        {
            if(message == null)
            {
                throw new ArgumentNullException("message");
            }

            var typeName = serializeType(typeof (T));
            var messageBody = serializer.MessageToBytes(message);

            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(
                    exchange: typeName,
                    type: ExchangeType.Direct,
                    durable: true);

                var defaultProperties = channel.CreateBasicProperties();
                channel.BasicPublish(
                    exchange: typeName, 
                    routingKey: typeName, 
                    basicProperties: defaultProperties,
                    body: messageBody);

            }
        }

        public void Subscribe<T>(Action<T> onMessage)
        {
            if(onMessage == null)
            {
                throw new ArgumentNullException("onMessage");
            }

            var typeName = serializeType(typeof(T));

            var subscriberName = subscriberNameFromDelegate(onMessage);

            var channel = connection.CreateModel();
            channel.ExchangeDeclare(
                exchange: typeName,
                type: ExchangeType.Direct,
                durable: true);

            var queue = channel.QueueDeclare(
                queue: subscriberName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueBind(queue, typeName, typeName);  

            // TODO: how does the channel (IModel) get disposed?  
            var consumer = new CallbackConsumer(channel, 
                (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                {
                    var message = serializer.BytesToMessage<T>(body);
                    onMessage(message);
                });

            channel.BasicConsume(subscriberName, true, consumer);
        }

        public void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse)
        {
            if(request == null)
            {
                throw new ArgumentNullException("request");
            }
            if(onResponse == null)
            {
                throw new ArgumentNullException("onResponse");
            }

            var requestBody = serializer.MessageToBytes(request);

            var requestTypeName = serializeType(typeof(TRequest));
            var requestChannel = connection.CreateModel();

            // respond queue is transient, only exists for the lifetime of the call.
            var respondQueue = requestChannel.QueueDeclare();

            // tell the consumer to respond to the transient respondQueue
            var requestProperties = requestChannel.CreateBasicProperties();
            requestProperties.ReplyTo = respondQueue;

            // should I declare the request queue here?
            Console.WriteLine("Making request to queue: {0}", requestTypeName);
            requestChannel.BasicPublish(
                exchange: rpcExchange, 
                routingKey: requestTypeName, 
                basicProperties: requestProperties, 
                body: requestBody);

            var consumer = new CallbackConsumer(requestChannel,
                (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                {
                    var response = serializer.BytesToMessage<TResponse>(body);
                    onResponse(response);
                    requestChannel.Dispose();
                });

            requestChannel.BasicConsume(queue: respondQueue, noAck: true, consumer: consumer);
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
        {
            if(responder == null)
            {
                throw new ArgumentNullException("responder");
            }

            var requestTypeName = serializeType(typeof(TRequest));
            var requestChannel = connection.CreateModel();
            requestChannel.ExchangeDeclare(
                exchange: rpcExchange, 
                type: ExchangeType.Direct, 
                autoDelete: false, 
                durable: true, 
                arguments: null);

            requestChannel.QueueDeclare(
                queue: requestTypeName, 
                durable: true, 
                exclusive: false, 
                autoDelete: false, 
                arguments: null);

            requestChannel.QueueBind(
                queue: requestTypeName,
                exchange: rpcExchange, 
                routingKey: requestTypeName);

            var consumer = new CallbackConsumer(requestChannel,
                (consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body) =>
                {
                    var request = serializer.BytesToMessage<TRequest>(body);
                    var response = responder(request);
                    var responseProperties = requestChannel.CreateBasicProperties();
                    var responseBody = serializer.MessageToBytes(response);
                    requestChannel.BasicPublish(
                        exchange: "", 
                        routingKey: properties.ReplyTo, 
                        basicProperties: responseProperties, 
                        body: responseBody);
                });

            // TODO: dispose channel
            requestChannel.BasicConsume(queue: requestTypeName, noAck: true, consumer: consumer);
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            
            connection.Close();
            connection.Dispose();
            disposed = true;
        }
    }
}