using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ.Logging;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer
{

    /// <summary>
    /// A strategy for dealing with failed messages. When a message consumer throws, HandleConsumerError is invoked.
    /// 
    /// The general principle is to put all failed messages in a dedicated error queue so that they can be 
    /// examined and retried (or ignored).
    /// 
    /// Each failed message is wrapped in a special system message, 'Error' and routed by a special exchange
    /// named after the original message's routing key. This is so that ad-hoc queues can be attached for
    /// errors on specific message types.
    /// 
    /// Each exchange is bound to the central EasyNetQ error queue.
    /// </summary>
    public class DefaultConsumerErrorStrategy : IConsumerErrorStrategy
    {
        private readonly ILog logger = LogProvider.For<DefaultConsumerErrorStrategy>();
        private readonly IConnectionFactory connectionFactory;
        private readonly ISerializer serializer;
        private readonly IConventions conventions;
        private readonly ITypeNameSerializer typeNameSerializer;
        private readonly IErrorMessageSerializer errorMessageSerializer;

        private readonly object syncLock = new object();

        private IConnection connection;
        private readonly ConcurrentDictionary<string, object> existingErrorExchangesWithQueues = new ConcurrentDictionary<string, object>();

        public DefaultConsumerErrorStrategy(
            IConnectionFactory connectionFactory,
            ISerializer serializer,
            IConventions conventions,
            ITypeNameSerializer typeNameSerializer,
            IErrorMessageSerializer errorMessageSerializer)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(serializer, "serializer");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.connectionFactory = connectionFactory;
            this.serializer = serializer;
            this.conventions = conventions;
            this.typeNameSerializer = typeNameSerializer;
            this.errorMessageSerializer = errorMessageSerializer;
        }

        protected void Connect()
        {
            if (connection == null || !connection.IsOpen)
            {
                lock (syncLock)
                {
                    if ((connection == null || !connection.IsOpen) && !(disposing || disposed))
                    {
                        if (connection != null)
                        {
                            try
                            {
                                // when the broker connection is lost
                                // and ShutdownReport.Count > 0 RabbitMQ.Client
                                // throw an exception, but before the IConnection.Abort()
                                // is invoked
                                // https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/ea913903602ba03841f2515c23f843304211cd9e/projects/client/RabbitMQ.Client/src/client/impl/Connection.cs#L1070
                                connection.Dispose();
                            }
                            catch
                            {
                                if (connection.CloseReason != null)
                                {
                                    logger.InfoFormat("Connection {connection} has shutdown with reason={reason}", connection.ToString(), connection.CloseReason.Cause);
                                }
                                else
                                {
                                    throw;
                                }
                            }

                        }

                        connection = connectionFactory.CreateConnection();
                        // A possible race condition exists during EasyNetQ IBus disposal where this instance's Dispose() method runs on another thread, while this thread is mid-executing connectionFactory.CreateConnection() (or a few lines earlier), and has not assigned the result to the connection variable before Dispose() accesses it.  This could result in the creation of a RabbitMQ.Client.IConnection instance which never gets disposed; that IConnection instance holds a thread open which can prevent application shutdown.  It was not desirable to lock (syncLock) within the Dispose() method to fix this; therefore, an additional check must be made here to dispose any such extra IConnection.
                        if (disposing || disposed)
                        {
                            connection.Dispose();
                        }
                    }
                }
            }
        }

        private static void DeclareAndBindErrorExchangeWithErrorQueue(IModel model, string exchangeName, string queueName, string routingKey)
        {
            model.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            model.ExchangeDeclare(exchangeName, ExchangeType.Direct, durable: true);
            model.QueueBind(queueName, exchangeName, routingKey);
        }

        private string DeclareErrorExchangeWithQueue(IModel model, ConsumerExecutionContext context)
        {
            var errorExchangeName = conventions.ErrorExchangeNamingConvention(context.Info);
            var errorQueueName = conventions.ErrorQueueNamingConvention(context.Info);
            var routingKey = context.Info.RoutingKey;

            var errorTopologyIdentifier = $"{errorExchangeName}-{errorQueueName}-{routingKey}";

            existingErrorExchangesWithQueues.GetOrAdd(errorTopologyIdentifier, _ =>
            {
                DeclareAndBindErrorExchangeWithErrorQueue(model, errorExchangeName, errorQueueName, routingKey);
                return null;
            });

            return errorExchangeName;
        }

        public virtual AckStrategy HandleConsumerError(ConsumerExecutionContext context, Exception exception)
        {
            Preconditions.CheckNotNull(context, "context");
            Preconditions.CheckNotNull(exception, "exception");

            if (disposed || disposing)
            {
                logger.ErrorFormat(
                    "ErrorStrategy was already disposed, when attempting to handle consumer error. Error message will not be published and message with receivedInfo={receivedInfo} will be requeued",
                    context.Info
                );

                return AckStrategies.NackWithRequeue;
            }

            try
            {
                Connect();

                using (var model = connection.CreateModel())
                {
                    var errorExchange = DeclareErrorExchangeWithQueue(model, context);

                    var messageBody = CreateErrorMessage(context, exception);
                    var properties = model.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.Type = typeNameSerializer.Serialize(typeof(Error));

                    model.BasicPublish(errorExchange, context.Info.RoutingKey, properties, messageBody);

                    return AckStrategies.Ack;
                }
            }
            catch (BrokerUnreachableException unreachableException)
            {
                // thrown if the broker is unreachable during initial creation.
                logger.Error(
                    unreachableException,
                    "Cannot connect to broker while attempting to publish error message"
                );
            }
            catch (OperationInterruptedException interruptedException)
            {
                // thrown if the broker connection is broken during declare or publish.
                logger.Error(
                    interruptedException,
                    "Broker connection was closed while attempting to publish error message"
                );
            }
            catch (Exception unexpectedException)
            {
                // Something else unexpected has gone wrong :(
                logger.Error(unexpectedException, "Failed to publish error message");
            }

            return AckStrategies.NackWithRequeue;
        }

        public AckStrategy HandleConsumerCancelled(ConsumerExecutionContext context)
        {
            return AckStrategies.NackWithRequeue;
        }

        private byte[] CreateErrorMessage(ConsumerExecutionContext context, Exception exception)
        {
            var messageAsString = errorMessageSerializer.Serialize(context.Body);
            var error = new Error
            {
                RoutingKey = context.Info.RoutingKey,
                Exchange = context.Info.Exchange,
                Queue = context.Info.Queue,
                Exception = exception.ToString(),
                Message = messageAsString,
                DateTime = DateTime.UtcNow
            };

            if (context.Properties.Headers == null)
            {
                error.BasicProperties = context.Properties;
            }
            else
            {
                // we'll need to clone context.Properties as we are mutating the headers dictionary
                error.BasicProperties = (MessageProperties)context.Properties.Clone();

                // the RabbitMQClient implicitly converts strings to byte[] on sending, but reads them back as byte[]
                // we're making the assumption here that any byte[] values in the headers are strings
                // and all others are basic types. RabbitMq client generally throws a nasty exception if you try
                // to store anything other than basic types in headers anyway.

                //see http://hg.rabbitmq.com/rabbitmq-dotnet-client/file/tip/projects/client/RabbitMQ.Client/src/client/impl/WireFormatting.cs

                error.BasicProperties.Headers = context.Properties.Headers.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value is byte[] ? Encoding.UTF8.GetString((byte[])kvp.Value) : kvp.Value);
            }

            return serializer.MessageToBytes(error);
        }

        private bool disposed;
        private bool disposing;

        public virtual void Dispose()
        {
            if (disposed) return;
            disposing = true;

            if (connection != null) { connection.Dispose(); }

            disposed = true;
        }
    }
}
