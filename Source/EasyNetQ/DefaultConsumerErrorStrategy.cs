using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    /// <summary>
    /// A strategy for dealing with failed messages. When a message consumer thows, HandleConsumerError is invoked.
    /// 
    /// The general priciple is to put all failed messages in a dedicated error queue so that they can be 
    /// examined and retried (or ignored).
    /// 
    /// Each failed message is wrapped in a special system message, 'Error' and routed by a special exchange
    /// named after the orignal message's routing key. This is so that ad-hoc queues can be attached for
    /// errors on specific message types.
    /// 
    /// Each exchange is bound to the central EasyNetQ error queue.
    /// </summary>
    public class DefaultConsumerErrorStrategy : IConsumerErrorStrategy
    {
        public const string EasyNetQErrorQueue = "EasyNetQ_Default_Error_Queue";
        public const string ErrorExchangePrefix = "ErrorExchange_";

        private readonly IConnectionFactory connectionFactory;
        private readonly ISerializer serializer;
        private readonly IEasyNetQLogger logger;
        private IConnection connection;
        private bool errorQueueDeclared = false;
        private readonly IDictionary<string, string> errorExchanges = new Dictionary<string, string>();

        public DefaultConsumerErrorStrategy(
            IConnectionFactory connectionFactory, 
            ISerializer serializer,
            IEasyNetQLogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.serializer = serializer;
            this.logger = logger;
        }

        private void Connect()
        {
            if(connection == null || !connection.IsOpen)
            {
                connection = connectionFactory.CreateConnection();
            }
        }

        private void DeclareDefaultErrorQueue(IModel model)
        {
            if (!errorQueueDeclared)
            {
                model.QueueDeclare(
                    queue: EasyNetQErrorQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                errorQueueDeclared = true;
            }
        }

        private string DeclareErrorExchangeAndBindToDefaultErrorQueue(IModel model, string originalRoutingKey)
        {
            if (!errorExchanges.ContainsKey(originalRoutingKey))
            {
                var exchangeName = ErrorExchangePrefix + originalRoutingKey;
                model.ExchangeDeclare(exchangeName, ExchangeType.Direct, durable:true);    
                model.QueueBind(EasyNetQErrorQueue, exchangeName, originalRoutingKey);

                errorExchanges.Add(originalRoutingKey, exchangeName);
            }

            return errorExchanges[originalRoutingKey];
        }

        private string DeclareErrorExchangeQueueStructure(IModel model, string originalRoutingKey)
        {
            DeclareDefaultErrorQueue(model);
            return DeclareErrorExchangeAndBindToDefaultErrorQueue(model, originalRoutingKey);
        }

        public void HandleConsumerError(BasicDeliverEventArgs devliverArgs, Exception exception)
        {
            try
            {
                Connect();

                using (var model = connection.CreateModel())
                {
                    var errorExchange = DeclareErrorExchangeQueueStructure(model, devliverArgs.RoutingKey);

                    var messageBody = CreateErrorMessage(devliverArgs, exception);
                    var properties = model.CreateBasicProperties();
                    properties.SetPersistent(true);

                    model.BasicPublish(errorExchange, devliverArgs.RoutingKey, properties, messageBody);
                }
            }
            catch (BrokerUnreachableException)
            {
                // thrown if the broker is unreachable during initial creation.
                logger.ErrorWrite("EasyNetQ Consumer Error Handler cannot connect to Broker\n" +
                    CreateConnectionCheckMessage());
            }
            catch (OperationInterruptedException interruptedException)
            {
                // thrown if the broker connection is broken during declare or publish.
                logger.ErrorWrite("EasyNetQ Consumer Error Handler: Broker connection was closed while attempting to publish Error message.\n" +
                    string.Format("Message was: '{0}'\n", interruptedException.Message) +
                    CreateConnectionCheckMessage());                
            }
            catch (Exception unexpecctedException)
            {
                // Something else unexpected has gone wrong :(
                logger.ErrorWrite("EasyNetQ Consumer Error Handler: Failed to publish error message\nException is:\n"
                    + unexpecctedException);
            }
        }

        private byte[] CreateErrorMessage(BasicDeliverEventArgs devliverArgs, Exception exception)
        {
            var messageAsString = Encoding.UTF8.GetString(devliverArgs.Body);
            var error = new Error
            {
                RoutingKey = devliverArgs.RoutingKey,
                Exchange = devliverArgs.Exchange,
                Exception = exception.ToString(),
                Message = messageAsString,
                DateTime = DateTime.Now,
                BasicProperties = new MessageBasicProperties(devliverArgs.BasicProperties)
            };

            return serializer.MessageToBytes(error);
        }

        private string CreateConnectionCheckMessage()
        {
            return
                "Please check EasyNetQ connection information and that the RabbitMQ Service is running at the specified endpoint.\n" +
                string.Format("\tHostname: '{0}'\n", connectionFactory.CurrentHost.Host) +
                string.Format("\tVirtualHost: '{0}'\n", connectionFactory.Configuration.VirtualHost) +
                string.Format("\tUserName: '{0}'\n", connectionFactory.Configuration.UserName) +
                "Failed to write error message to error queue";
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;

            if(connection != null) connection.Dispose();

            disposed = true;
        }
    }
}