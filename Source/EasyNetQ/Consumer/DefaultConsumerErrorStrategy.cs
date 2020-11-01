using System;
using System.Collections.Concurrent;
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
        private readonly IPersistentConnection connection;
        private readonly IConventions conventions;
        private readonly IErrorMessageSerializer errorMessageSerializer;
        private readonly ConcurrentDictionary<string, object> existingErrorExchangesWithQueues = new ConcurrentDictionary<string, object>();
        private readonly ILog logger = LogProvider.For<DefaultConsumerErrorStrategy>();
        private readonly ISerializer serializer;
        private readonly ITypeNameSerializer typeNameSerializer;
        private readonly ConnectionConfiguration configuration;

        private bool disposed;
        private bool disposing;

        public DefaultConsumerErrorStrategy(
            IPersistentConnection connection,
            ISerializer serializer,
            IConventions conventions,
            ITypeNameSerializer typeNameSerializer,
            IErrorMessageSerializer errorMessageSerializer,
            ConnectionConfiguration configuration
        )
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(serializer, "serializer");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.connection = connection;
            this.serializer = serializer;
            this.conventions = conventions;
            this.typeNameSerializer = typeNameSerializer;
            this.errorMessageSerializer = errorMessageSerializer;
            this.configuration = configuration;
        }

        /// <inheritdoc />
        public virtual AckStrategy HandleConsumerError(ConsumerExecutionContext context, Exception exception)
        {
            Preconditions.CheckNotNull(context, "context");
            Preconditions.CheckNotNull(exception, "exception");

            if (disposed || disposing)
            {
                logger.ErrorFormat(
                    "ErrorStrategy was already disposed, when attempting to handle consumer error. Error message will not be published and message with receivedInfo={receivedInfo} will be requeued",
                    context.ReceivedInfo
                );

                return AckStrategies.NackWithRequeue;
            }

            logger.Error(
                exception,
                "Exception thrown by subscription callback, receivedInfo={receivedInfo}, properties={properties}, message={message}",
                context.ReceivedInfo,
                context.Properties,
                Convert.ToBase64String(context.Body)
            );

            try
            {
                using var model = connection.CreateModel();
                if (configuration.PublisherConfirms) model.ConfirmSelect();

                var errorExchange = DeclareErrorExchangeWithQueue(model, context);

                var messageBody = CreateErrorMessage(context, exception);
                var properties = model.CreateBasicProperties();
                properties.Persistent = true;
                properties.Type = typeNameSerializer.Serialize(typeof(Error));

                model.BasicPublish(errorExchange, context.ReceivedInfo.RoutingKey, properties, messageBody);

                if (!configuration.PublisherConfirms) return AckStrategies.Ack;

                return model.WaitForConfirms(configuration.Timeout) ? AckStrategies.Ack : AckStrategies.NackWithRequeue;
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

        /// <inheritdoc />
        public virtual AckStrategy HandleConsumerCancelled(ConsumerExecutionContext context)
        {
            return AckStrategies.NackWithRequeue;
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if (disposed) return;
            disposing = true;

            connection.Dispose();

            disposed = true;
        }

        private static void DeclareAndBindErrorExchangeWithErrorQueue(IModel model, string exchangeName, string queueName, string routingKey)
        {
            model.QueueDeclare(queueName, true, false, false, null);
            model.ExchangeDeclare(exchangeName, ExchangeType.Direct, true);
            model.QueueBind(queueName, exchangeName, routingKey);
        }

        private string DeclareErrorExchangeWithQueue(IModel model, ConsumerExecutionContext context)
        {
            var errorExchangeName = conventions.ErrorExchangeNamingConvention(context.ReceivedInfo);
            var errorQueueName = conventions.ErrorQueueNamingConvention(context.ReceivedInfo);
            var routingKey = context.ReceivedInfo.RoutingKey;

            var errorTopologyIdentifier = $"{errorExchangeName}-{errorQueueName}-{routingKey}";

            existingErrorExchangesWithQueues.GetOrAdd(errorTopologyIdentifier, _ =>
            {
                DeclareAndBindErrorExchangeWithErrorQueue(model, errorExchangeName, errorQueueName, routingKey);
                return null;
            });

            return errorExchangeName;
        }

        private byte[] CreateErrorMessage(ConsumerExecutionContext context, Exception exception)
        {
            var messageAsString = errorMessageSerializer.Serialize(context.Body);
            var error = new Error
            {
                RoutingKey = context.ReceivedInfo.RoutingKey,
                Exchange = context.ReceivedInfo.Exchange,
                Queue = context.ReceivedInfo.Queue,
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
                    kvp => kvp.Value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : kvp.Value);
            }

            return serializer.MessageToBytes(typeof(Error), error);
        }
    }
}
