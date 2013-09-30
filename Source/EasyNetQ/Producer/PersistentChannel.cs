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

        private IModel channel;
        private bool disconnected = true;

        public PersistentChannel(IPersistentConnection connection, IEasyNetQLogger logger)
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
                        WaitForReconnection();
                        InvokeChannelAction(channelAction);
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

        private void WaitForReconnection()
        {
            // TODO: timeout
            logger.DebugWrite("Persistent channel operation failed. Waiting for reconnection.");
            while (disconnected)
            {
                Thread.Sleep(100);
            }
        }
    }
}