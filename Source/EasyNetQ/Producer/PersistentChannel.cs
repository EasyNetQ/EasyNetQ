using System;
using System.Threading;
using EasyNetQ.AmqpExceptions;
using EasyNetQ.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Producer
{
    public class PersistentChannel : IPersistentChannel
    {
        private readonly IPersistentConnection connection;
        private readonly IEasyNetQLogger logger;
        private readonly ConnectionConfiguration configuration;
        private readonly IEventBus eventBus;

        private IModel channel;
        private bool disconnected = true;

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

        private void WireUpEvents()
        {
            eventBus.Subscribe<ConnectionDisconnectedEvent>(OnConnectionDisconnected);
            eventBus.Subscribe<ConnectionCreatedEvent>(ConnectionOnConnected);
        }

        private void OnConnectionDisconnected(ConnectionDisconnectedEvent @event)
        {
            if (!disconnected)
            {
                disconnected = true;
                channel = null;
                logger.DebugWrite("Persistent channel disconnected.");
            }
        }

        private void ConnectionOnConnected(ConnectionCreatedEvent @event)
        {
            OpenChannel();
        }

        public IModel Channel
        {
            get
            {
                if (channel == null)
                {
                    OpenChannel();
                }
                return channel;
            }
        }

        private void OpenChannel()
        {
            channel = connection.CreateModel();
            disconnected = false;
            eventBus.Publish(new PublishChannelCreatedEvent(channel));
            logger.DebugWrite("Persistent channel connected.");
        }

        public void InvokeChannelAction(Action<IModel> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
            InvokeChannelActionInternal(channelAction, DateTime.Now);
        }

        private void InvokeChannelActionInternal(Action<IModel> channelAction, DateTime startTime)
        {
            if (IsTimedOut(startTime))
            {
                logger.ErrorWrite("Channel action timed out. Throwing exception to client.");
                throw new TimeoutException("The operation requested on PersistentChannel timed out.");
            }
            try
            {
                channelAction(Channel);
            }
            catch (OperationInterruptedException exception)
            {
                try
                {
                    var amqpException = AmqpExceptionGrammar.ParseExceptionString(exception.Message);
                    if (amqpException.Code == AmqpException.ConnectionClosed)
                    {
                        OnConnectionDisconnected(null);
                        WaitForReconnectionOrTimeout(startTime);
                        InvokeChannelActionInternal(channelAction, startTime);
                    }
                    else
                    {
                        OpenChannel();
                        throw;
                    }
                }
                catch (Sprache.ParseException)
                {
                    throw exception;
                }
            }
            catch (EasyNetQException)
            {
                OnConnectionDisconnected(null);
                WaitForReconnectionOrTimeout(startTime);
                InvokeChannelActionInternal(channelAction, startTime);
            }

        }

        private void WaitForReconnectionOrTimeout(DateTime startTime)
        {
            logger.DebugWrite("Persistent channel operation failed. Waiting for reconnection.");
            var delayMilliseconds = 10;

            while (disconnected && !IsTimedOut(startTime))
            {
                Thread.Sleep(delayMilliseconds);
                delayMilliseconds *= 2; // back off exponentially
                try
                {
                    OpenChannel();
                }
                catch (OperationInterruptedException)
                {}
                catch (EasyNetQException)
                {}
            }
        }

        private bool IsTimedOut(DateTime startTime)
        {
            return !configuration.Timeout.Equals(0) && startTime.AddSeconds(configuration.Timeout) < DateTime.Now;
        }

        public void Dispose()
        {
            if (channel != null)
            {
                channel.Dispose();
                logger.DebugWrite("Persistent channel disposed.");
            }
        }
    }
}