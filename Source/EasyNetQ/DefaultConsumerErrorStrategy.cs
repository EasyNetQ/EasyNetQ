using System;
using System.Collections.Concurrent;
using System.Text;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client;
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
        private readonly IConnectionFactory connectionFactory;
        private readonly ISerializer serializer;
        private readonly IEasyNetQLogger logger;
        private readonly IConventions conventions;
        private IConnection connection;
        private bool errorQueueDeclared = false;
        private readonly ConcurrentDictionary<string, string> errorExchanges = new ConcurrentDictionary<string, string>();

        public DefaultConsumerErrorStrategy(
            IConnectionFactory connectionFactory, 
            ISerializer serializer,
            IEasyNetQLogger logger,
            IConventions conventions)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(serializer, "serializer");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(conventions, "conventions");

            this.connectionFactory = connectionFactory;
            this.serializer = serializer;
            this.logger = logger;
            this.conventions = conventions;
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
                    queue: conventions.ErrorQueueNamingConvention(),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                errorQueueDeclared = true;
            }
        }

        private string DeclareErrorExchangeAndBindToDefaultErrorQueue(IModel model, string originalRoutingKey)
        {
            return errorExchanges.GetOrAdd(originalRoutingKey, _ =>
            {
                var exchangeName = conventions.ErrorExchangeNamingConvention(originalRoutingKey);
                model.ExchangeDeclare(exchangeName, ExchangeType.Direct, durable: true);
                model.QueueBind(conventions.ErrorQueueNamingConvention(), exchangeName, originalRoutingKey);
                return exchangeName;
            });
        }

        private string DeclareErrorExchangeQueueStructure(IModel model, string originalRoutingKey)
        {
            DeclareDefaultErrorQueue(model);
            return DeclareErrorExchangeAndBindToDefaultErrorQueue(model, originalRoutingKey);
        }

        public virtual void HandleConsumerError(ConsumerExecutionContext context, Exception exception)
        {
            Preconditions.CheckNotNull(context, "context");
            Preconditions.CheckNotNull(exception, "exception");

            try
            {
                Connect();

                using (var model = connection.CreateModel())
                {
                    var errorExchange = DeclareErrorExchangeQueueStructure(model, context.Info.RoutingKey);

                    var messageBody = CreateErrorMessage(context, exception);
                    var properties = model.CreateBasicProperties();
                    properties.SetPersistent(true);
                    properties.Type = TypeNameSerializer.Serialize(typeof (Error));

                    model.BasicPublish(errorExchange, context.Info.RoutingKey, properties, messageBody);
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

        public virtual PostExceptionAckStrategy PostExceptionAckStrategy()
        {
            return EasyNetQ.PostExceptionAckStrategy.ShouldAck;
        }

        private byte[] CreateErrorMessage(ConsumerExecutionContext context, Exception exception)
        {
            var messageAsString = Encoding.UTF8.GetString(context.Body);
            var error = new Error
            {
                RoutingKey = context.Info.RoutingKey,
                Exchange = context.Info.Exchange,
                Exception = exception.ToString(),
                Message = messageAsString,
                DateTime = DateTime.Now,
                // TODO:
                // BasicProperties = context.Properties
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

        public virtual void Dispose()
        {
            if (disposed) return;

            if(connection != null) connection.Dispose();

            disposed = true;
        }
    }
}
