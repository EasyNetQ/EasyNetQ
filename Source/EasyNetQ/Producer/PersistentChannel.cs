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
            this.connection = connection;

            this.connection.Disconnected += () => 
                {
                    channel = null;
                    disconnected = true;
                };

            this.connection.Connected += () =>
                {
                    try
                    {
                        channel = connection.CreateModel();
                        disconnected = false;
                    }
                    catch (Exception e)
                    {
                        logger.ErrorWrite(e);
                    }
                };

            this.logger = logger;
            this.configuration = configuration;
        }

        public IModel Channel
        {
            get
            {
                if (channel == null || !channel.IsOpen)
                {
                    channel = connection.CreateModel();
                    disconnected = false;
                }
                return channel;
            }
        }

        public void Dispose()
        {
            if(channel != null) channel.Dispose();
        }

        public void InvokeChannelAction(Action<IModel> channelAction)
        {
            InvokeChannelActionInternal(channelAction, DateTime.Now);
        }

        private void InvokeChannelActionInternal(Action<IModel> channelAction, DateTime startTime)
        {
            if (IsTimedOut(startTime))
            {
                logger.ErrorWrite("Channel action timed out. Throwing exception to client.");
                throw new TimeoutException("The operation requested on PersistentChannel could not be completed.");
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
            while (disconnected && !IsTimedOut(startTime))
            {
                Thread.Sleep(100);
            }
        }

        private bool IsTimedOut(DateTime startTime)
        {
            return startTime.AddSeconds(configuration.Timeout) < DateTime.Now;
        }
    }
}