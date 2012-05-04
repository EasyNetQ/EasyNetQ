using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    public class RabbitPublishChannel : IPublishChannel, IRawByteBus
    {
        private readonly IPersistentConnection connection;
        private readonly IEasyNetQLogger logger;
        private readonly Func<string> getCorrelationId;
        private readonly ISet<string> publishExchanges;
        private readonly SerializeType serializeType;
        private readonly ISerializer serializer;
        private readonly IConventions conventions;

        private readonly IModel channel;

        public RabbitPublishChannel(
            IPersistentConnection connection, 
            IEasyNetQLogger logger, 
            Func<string> getCorrelationId, 
            ISet<string> publishExchanges, 
            SerializeType serializeType, 
            ISerializer serializer, 
            IConventions conventions)
        {
            this.connection = connection;
            this.logger = logger;
            this.getCorrelationId = getCorrelationId;
            this.publishExchanges = publishExchanges;
            this.serializeType = serializeType;
            this.serializer = serializer;
            this.conventions = conventions;

            channel = connection.CreateModel();
        }

        public void Publish<T>(T message)
        {
            Publish(conventions.TopicNamingConvention(typeof(T)), message);
        }

        public void Publish<T>(string topic, T message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            var typeName = serializeType(typeof(T));
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var messageBody = serializer.MessageToBytes(message);

            RawPublish(exchangeName, topic, typeName, messageBody);
        }

        public void RawPublish(string exchangeName, string topic, string typeName, byte[] messageBody)
        {
            if (disposed)
            {
                throw new EasyNetQException("PublishChannel is already disposed");
            }

            if (!connection.IsConnected)
            {
                throw new EasyNetQException("Publish failed. No rabbit server connected.");
            }

            try
            {
                DeclarePublishExchange(exchangeName);

                var defaultProperties = channel.CreateBasicProperties();
                defaultProperties.SetPersistent(false);
                defaultProperties.Type = typeName;
                defaultProperties.CorrelationId = getCorrelationId();

                channel.BasicPublish(
                    exchangeName, // exchange
                    topic, // routingKey 
                    defaultProperties, // basicProperties
                    messageBody); // body

                logger.DebugWrite("Published {0}, CorrelationId {1}", exchangeName, defaultProperties.CorrelationId);
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
            if (!publishExchanges.Contains(exchangeName))
            {
                channel.ExchangeDeclare(
                    exchangeName,               // exchange
                    ExchangeType.Topic,    // type
                    true);                  // durable

                publishExchanges.Add(exchangeName);
            }
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            channel.Dispose();
            disposed = true;
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
    }
}