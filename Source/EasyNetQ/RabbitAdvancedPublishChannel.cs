using System;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    public class RabbitAdvancedPublishChannel : IAdvancedPublishChannel
    {
        private readonly RabbitAdvancedBus advancedBus;
        private readonly IModel channel;

        public RabbitAdvancedPublishChannel(RabbitAdvancedBus advancedBus)
        {
            if(advancedBus == null)
            {
                throw new ArgumentNullException("advancedBus");
            }
            if (!advancedBus.Connection.IsConnected)
            {
                throw new EasyNetQException("Cannot open channel for publishing, the broker is not connected");
            }

            this.advancedBus = advancedBus;
            channel = advancedBus.Connection.CreateModel();
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            channel.Abort();
            channel.Dispose();
            disposed = true;
        }

        public void Publish<T>(IExchange exchange, string routingKey, IMessage<T> message)
        {
            if(exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if(routingKey == null)
            {
                throw new ArgumentNullException("routingKey");
            }
            if(message == null)
            {
                throw new ArgumentNullException("message");
            }

            var typeName = advancedBus.SerializeType(typeof(T));
            var messageBody = advancedBus.Serializer.MessageToBytes(message.Body);

            // message.Properties.persistent = true;
            message.Properties.Type = typeName;
            message.Properties.CorrelationId = advancedBus.GetCorrelationId();

            Publish(exchange, routingKey, message.Properties, messageBody);
        }

        public void Publish(IExchange exchange, string routingKey, MessageProperties properties, byte[] messageBody)
        {
            if (exchange == null)
            {
                throw new ArgumentNullException("exchange");
            }
            if (routingKey == null)
            {
                throw new ArgumentNullException("routingKey");
            }
            if(properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            if(messageBody == null)
            {
                throw new ArgumentNullException("messageBody");
            }
            if (disposed)
            {
                throw new EasyNetQException("PublishChannel is already disposed");
            }
            if (!advancedBus.Connection.IsConnected)
            {
                throw new EasyNetQException("Publish failed. No rabbit server connected.");
            }
            try
            {
                var defaultProperties = channel.CreateBasicProperties();
                properties.CopyTo(defaultProperties);

                exchange.Visit(new TopologyBuilder(channel));

                channel.BasicPublish(
                    exchange.Name,      // exchange
                    routingKey,         // routingKey 
                    defaultProperties,  // basicProperties
                    messageBody);       // body

                advancedBus.Logger.DebugWrite("Published {0}, CorrelationId {1}", exchange.Name, defaultProperties.CorrelationId);
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
    }
}