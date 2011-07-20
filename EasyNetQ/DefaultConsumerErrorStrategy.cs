using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

        private readonly ConnectionFactory connectionFactory;
        private readonly ISerializer serializer;
        private IConnection connection;
        private bool errorQueueDeclared = false;
        private readonly IDictionary<string, string> errorExchanges = new Dictionary<string, string>();

        public DefaultConsumerErrorStrategy(ConnectionFactory connectionFactory, ISerializer serializer)
        {
            this.connectionFactory = connectionFactory;
            this.serializer = serializer;
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

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;

            if(connection != null) connection.Dispose();

            disposed = true;
        }
    }
}