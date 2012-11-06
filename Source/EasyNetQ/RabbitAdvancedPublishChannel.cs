using System;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    public class RabbitAdvancedPublishChannel : IAdvancedPublishChannel
    {
        private readonly RabbitAdvancedBus advancedBus;
        private readonly IModel channel;
        private readonly ChannelConfiguration channelConfiguration;
        private readonly IPublisherConfirms publisherConfirms;

        public RabbitAdvancedPublishChannel(RabbitAdvancedBus advancedBus, Action<IChannelConfiguration> configure)
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

            channelConfiguration = new ChannelConfiguration();
            configure(channelConfiguration);

            publisherConfirms = ConfigureChannel(channelConfiguration, channel);
        }

        private static IPublisherConfirms ConfigureChannel(ChannelConfiguration configuration, IModel channel)
        {
            if (configuration.PublisherConfirmsOn)
            {
                channel.ConfirmSelect();
                var publisherConfirms = new PublisherConfirms();
                channel.BasicAcks += publisherConfirms.SuccessfulPublish;
                channel.BasicNacks += publisherConfirms.FailedPublish;
                return publisherConfirms;
            }
            return null;
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            channel.Abort();
            channel.Dispose();
            disposed = true;
        }

        public void Publish<T>(IExchange exchange, string routingKey, IMessage<T> message, Action<IAdvancedPublishConfiguration> configure)
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
            if(configure == null)
            {
                throw new ArgumentNullException("configure");
            }

            var typeName = advancedBus.SerializeType(typeof(T));
            var messageBody = advancedBus.Serializer.MessageToBytes(message.Body);

            message.Properties.Type = typeName;
            message.Properties.CorrelationId = 
                string.IsNullOrEmpty(message.Properties.CorrelationId) ?
                advancedBus.GetCorrelationId() : 
                message.Properties.CorrelationId;

            Publish(exchange, routingKey, message.Properties, messageBody, configure);
        }

        public void Publish(IExchange exchange, string routingKey, MessageProperties properties, byte[] messageBody, Action<IAdvancedPublishConfiguration> configure)
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
            if(configure == null)
            {
                throw new ArgumentNullException("configure");
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
                var configuration = new AdvancedPublishConfiguration();
                configure(configuration);

                if (publisherConfirms != null)
                {
                    if (configuration.SuccessCallback == null || configuration.FailureCallback == null)
                    {
                        throw new EasyNetQException("When pulisher confirms are on, you must supply success and failure callbacks in the publish configuration");
                    }

                    publisherConfirms.RegisterCallbacks(channel, configuration.SuccessCallback, configuration.FailureCallback);
                }

                var defaultProperties = channel.CreateBasicProperties();
                properties.CopyTo(defaultProperties);

                exchange.Visit(new TopologyBuilder(channel));

                channel.BasicPublish(
                    exchange.Name,      // exchange
                    routingKey,         // routingKey 
                    defaultProperties,  // basicProperties
                    messageBody);       // body

                advancedBus.Logger.DebugWrite("Published to exchange: '{0}', routing key: '{1}', correlationId: '{2}'", 
                    exchange.Name, routingKey, defaultProperties.CorrelationId);
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

        public void Publish<T>(IExchange exchange, string routingKey, IMessage<T> message)
        {
            Publish(exchange, routingKey, message, configuration => {});
        }

        public void Publish(IExchange exchange, string routingKey, MessageProperties properties, byte[] messageBody)
        {
            Publish(exchange, routingKey, properties, messageBody, configuration => {});
        }
    }
}