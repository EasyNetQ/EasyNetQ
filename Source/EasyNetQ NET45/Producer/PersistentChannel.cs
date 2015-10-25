using System;
using System.Threading;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Sprache;

namespace EasyNetQ.Producer
{
    public class PersistentChannel : IPersistentChannel
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;
        private readonly IEasyNetQLogger logger;
        private IModel internalChannel;

        public PersistentChannel(
            IPersistentConnection connection,
            IEasyNetQLogger logger,
            ConnectionConfiguration configuration,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.connection = connection;
            this.logger = logger;
            this.configuration = configuration;
            this.eventBus = eventBus;

            WireUpEvents();
        }

        public void InvokeChannelAction(Action<IModel> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
            var startTime = DateTime.UtcNow;
            var retryTimeout = TimeSpan.FromMilliseconds(50);
            while (!IsTimedOut(startTime))
            {
                try
                {
                    var channel = OpenChannel();
                    channelAction(channel);
                    return;
                }
                catch (OperationInterruptedException exception)
                {
                    CloseChannel();
                    if (NeedRethrow(exception))
                    {
                        throw;
                    }
                }
                catch (EasyNetQException)
                {
                    CloseChannel();
                }

                Thread.Sleep(retryTimeout);

                retryTimeout = retryTimeout.Double();
            }
            logger.ErrorWrite("Channel action timed out. Throwing exception to client.");
            throw new TimeoutException("The operation requested on PersistentChannel timed out.");
        }

        public void Dispose()
        {
            CloseChannel();
            logger.DebugWrite("Persistent internalChannel disposed.");
        }

        private void WireUpEvents()
        {
            eventBus.Subscribe<ConnectionDisconnectedEvent>(OnConnectionDisconnected);
            eventBus.Subscribe<ConnectionCreatedEvent>(ConnectionOnConnected);
        }

        private void OnConnectionDisconnected(ConnectionDisconnectedEvent @event)
        {
            CloseChannel();
        }

        private void ConnectionOnConnected(ConnectionCreatedEvent @event)
        {
            try
            {
                OpenChannel();
            }
            catch (OperationInterruptedException)
            {
            }
            catch (EasyNetQException)
            {
            }
        }

        private IModel OpenChannel()
        {
            IModel channel;

            lock (this)
            {
                if (internalChannel != null)
                {
                    return internalChannel;
                }

                channel = connection.CreateModel();

                WireUpChannelEvents(channel);

                eventBus.Publish(new PublishChannelCreatedEvent(channel));

                internalChannel = channel;
            }

            logger.DebugWrite("Persistent channel connected.");
            return channel;
        }

        private void WireUpChannelEvents(IModel channel)
        {
            if (configuration.PublisherConfirms)
            {
                channel.ConfirmSelect();

                channel.BasicAcks += OnAck;
                channel.BasicNacks += OnNack;
            }

            channel.BasicReturn += OnReturn;
        }

        private void OnReturn(object sender, BasicReturnEventArgs args)
        {
            eventBus.Publish(new ReturnedMessageEvent(args.Body,
                new MessageProperties(args.BasicProperties),
                new MessageReturnedInfo(args.Exchange, args.RoutingKey, args.ReplyText)));
        }

        private void OnAck(object sender, BasicAckEventArgs args)
        {
            eventBus.Publish(MessageConfirmationEvent.Ack((IModel) sender, args.DeliveryTag, args.Multiple));
        }

        private void OnNack(object sender, BasicNackEventArgs args)
        {
            eventBus.Publish(MessageConfirmationEvent.Nack((IModel)sender, args.DeliveryTag, args.Multiple));
        }

        private void CloseChannel()
        {
            lock (this)
            {
                if (internalChannel == null)
                {
                    return;
                }
                if (configuration.PublisherConfirms)
                {
                    internalChannel.BasicAcks -= OnAck;
                    internalChannel.BasicNacks -= OnNack;
                }
                internalChannel.BasicReturn -= OnReturn;
                // Fix me: use Dispose instead of SafeDispose after update of Rabbitmq.Client to 3.5.5
                internalChannel.SafeDispose();
                internalChannel = null;
            }

            logger.DebugWrite("Persistent channel disconnected.");
        }

        private static bool NeedRethrow(OperationInterruptedException exception)
        {
            try
            {
                var amqpException = AmqpExceptionGrammar.ParseExceptionString(exception.Message);
                return amqpException.Code != AmqpException.ConnectionClosed;
            }
            catch (ParseException)
            {
                return true;
            }
        }

        private bool IsTimedOut(DateTime startTime)
        {
            return !configuration.Timeout.Equals(0) && startTime.AddSeconds(configuration.Timeout) < DateTime.UtcNow;
        }
    }
}