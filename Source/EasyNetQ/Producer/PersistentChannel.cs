﻿using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Events;
using EasyNetQ.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Sprache;

namespace EasyNetQ.Producer
{
    public class PersistentChannel : IPersistentChannel
    {
        private readonly ILog logger = LogProvider.For<PersistentChannel>();
        private readonly ConnectionConfiguration configuration;
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;
        private IModel internalChannel;

        public PersistentChannel(
            IPersistentConnection connection,
            ConnectionConfiguration configuration,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.connection = connection;
            this.configuration = configuration;
            this.eventBus = eventBus;

            WireUpEvents();
        }

        public T InvokeChannelAction<T>(Func<IModel, T> channelAction, CancellationToken cancellation = default(CancellationToken))
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            var startTime = DateTime.UtcNow;
            var retryTimeout = TimeSpan.FromMilliseconds(50);
            while (!IsTimedOut(startTime))
            {
                cancellation.ThrowIfCancellationRequested();

                try
                {
                    var channel = OpenChannel();
                    return channelAction(channel);
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

                Task.Delay(retryTimeout, cancellation)
                    .GetAwaiter()
                    .GetResult();

                retryTimeout = retryTimeout.Double();
            }
            
            logger.Error("Channel action timed out");
            throw new TimeoutException("The operation requested on PersistentChannel timed out");
        }

        public void InvokeChannelAction(Action<IModel> channelAction, CancellationToken cancellation = default(CancellationToken))
        {
            InvokeChannelAction<object>(x =>
                {
                    channelAction(x);
                    return null;
                }, cancellation);
        }

        public void Dispose()
        {
            CloseChannel();
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

            logger.Debug("Persistent channel connected");
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
                internalChannel = null;
            }

            logger.Debug("Persistent channel disconnected");
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