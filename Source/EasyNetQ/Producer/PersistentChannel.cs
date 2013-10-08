using System;
using System.Threading;
using EasyNetQ.AmqpExceptions;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Producer
{
    public class PersistentChannel : IPersistentChannel
    {
        private readonly IPersistentConnection connection;
        private readonly IEasyNetQLogger logger;
        private readonly IConnectionConfiguration configuration;

        private IModel channel;
        private bool disconnected = true;

        public PersistentChannel(
            IPersistentConnection connection, 
            IEasyNetQLogger logger, 
            IConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.connection = connection;
            this.logger = logger;
            this.configuration = configuration;

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            connection.Disconnected += OnConnectionDisconnected;
        }

        private void UnwireEvents()
        {
            connection.Disconnected -= OnConnectionDisconnected;
        }

        private void OnConnectionDisconnected()
        {
            if (!disconnected)
            {
                disconnected = true;
                channel = null;
                logger.DebugWrite("Persistent channel disconnected.");
            }
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
                        OnConnectionDisconnected();
                        WaitForReconnectionOrTimeout(startTime);
                        InvokeChannelActionInternal(channelAction, startTime);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Sprache.ParseException)
                {
                    throw exception;
                }
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
                {
                }
            }
        }

        private bool IsTimedOut(DateTime startTime)
        {
            return startTime.AddSeconds(configuration.Timeout) < DateTime.Now;
        }

        public void Dispose()
        {
            UnwireEvents();

            if (channel != null)
            {
                channel.Dispose();
                logger.DebugWrite("Persistent channel disposed.");
            }
        }
    }
}