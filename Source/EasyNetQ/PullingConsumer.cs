using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Interception;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Represents a result of a pull
    /// </summary>
    public interface IPullResult
    {
        /// <summary>
        ///     True if a message is available
        /// </summary>
        public bool IsAvailable { get; }

        /// <summary>
        ///     Returns remained messages count if the message is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public ulong MessagesCount { get; }

        /// <summary>
        ///     Returns received info if the message is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public MessageReceivedInfo ReceivedInfo { get; }
    }

    /// <summary>
    ///     Represents a result of a message pull
    /// </summary>
    public readonly struct PullResult : IPullResult
    {
        private readonly MessageReceivedInfo receivedInfo;
        private readonly MessageProperties properties;
        private readonly byte[] body;
        private readonly ulong messagesCount;

        /// <summary>
        ///     Represents a result when no message is available
        /// </summary>
        public static PullResult NotAvailable { get; } = new PullResult(false, 0, null, null, null);

        /// <summary>
        ///     Represents a result when a message is available
        /// </summary>
        /// <returns></returns>
        public static PullResult Available(
            ulong messagesCount, MessageReceivedInfo receivedInfo, MessageProperties properties, byte[] body
        )
        {
            return new PullResult(true, messagesCount, receivedInfo, properties, body);
        }

        private PullResult(
            bool isAvailable, ulong messagesCount, MessageReceivedInfo receivedInfo, MessageProperties properties, byte[] body
        )
        {
            IsAvailable = isAvailable;
            this.messagesCount = messagesCount;
            this.receivedInfo = receivedInfo;
            this.properties = properties;
            this.body = body;
        }

        /// <summary>
        ///     True if a message is available
        /// </summary>
        public bool IsAvailable { get; }

        /// <summary>
        ///     Returns remained messages count if the message is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public ulong MessagesCount
        {
            get
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("No message is available");

                return messagesCount;
            }
        }

        /// <summary>
        ///     Returns received info if the message is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public MessageReceivedInfo ReceivedInfo
        {
            get
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("No message is available");

                return receivedInfo;
            }
        }

        /// <summary>
        ///     Returns properties if the message is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public MessageProperties Properties
        {
            get
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("No message is available");

                return properties;
            }
        }

        /// <summary>
        ///     Returns body info if the message is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public byte[] Body
        {
            get
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("No message is available");

                return body;
            }
        }
    }

    /// <summary>
    ///     Represents a result of a message pull
    /// </summary>
    public readonly struct PullResult<T> : IPullResult
    {
        private readonly MessageReceivedInfo receivedInfo;
        private readonly IMessage<T> message;
        private readonly ulong messagesCount;

        /// <summary>
        ///     Represents a result when no message is available
        /// </summary>
        public static PullResult<T> NotAvailable { get; } = new PullResult<T>(false, 0, null, null);

        /// <summary>
        ///     Represents a result when a message is available
        /// </summary>
        /// <returns></returns>
        public static PullResult<T> Available(
            ulong messagesCount, MessageReceivedInfo receivedInfo, IMessage<T> message
        )
        {
            return new PullResult<T>(true, messagesCount, receivedInfo, message);
        }

        private PullResult(
            bool isAvailable,
            ulong messagesCount,
            MessageReceivedInfo receivedInfo,
            IMessage<T> message
        )
        {
            IsAvailable = isAvailable;
            this.messagesCount = messagesCount;
            this.receivedInfo = receivedInfo;
            this.message = message;
        }

        /// <summary>
        ///     True if a message is available
        /// </summary>
        public bool IsAvailable { get; }

        /// <summary>
        ///     Returns remained messages count if the message is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public ulong MessagesCount
        {
            get
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("No message is available");

                return messagesCount;
            }
        }

        /// <summary>
        ///     Returns received info if the message is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public MessageReceivedInfo ReceivedInfo
        {
            get
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("No message is available");

                return receivedInfo;
            }
        }

        /// <summary>
        ///     Returns message if it is available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public IMessage<T> Message
        {
            get
            {
                if (!IsAvailable)
                    throw new InvalidOperationException("No message is available");

                return message;
            }
        }
    }


    /// <summary>
    ///     Allows to receive messages by pulling them one by one
    /// </summary>
    public interface IPullingConsumer<TPullResult> : IDisposable where TPullResult : IPullResult
    {
        /// <summary>
        ///     Receives a single message
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        Task<TPullResult> PullAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Acknowledges one or more messages
        /// </summary>
        /// <param name="deliveryTag"></param>
        /// <param name="multiple"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Rejects one or more messages
        /// </summary>
        /// <param name="deliveryTag"></param>
        /// <param name="multiple"></param>
        /// <param name="requeue"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RejectAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///     Represent pulling consumer options
    /// </summary>
    public readonly struct PullingConsumerOptions
    {
        /// <summary>
        ///     True if auto ack is enabled for the consumer
        /// </summary>
        public bool AutoAck { get; }

        /// <summary>
        ///     Operations timeout
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        ///     Creates PullingConsumerOptions
        /// </summary>
        /// <param name="autoAck">The autoAck</param>
        /// <param name="timeout">The timeout</param>
        public PullingConsumerOptions(bool autoAck, TimeSpan timeout)
        {
            AutoAck = autoAck;
            Timeout = timeout;
        }
    }

    /// <inheritdoc />
    public class PullingConsumer : IPullingConsumer<PullResult>
    {
        private readonly IPersistentChannel channel;
        private readonly IProduceConsumeInterceptor interceptor;
        private readonly PullingConsumerOptions options;
        private readonly IQueue queue;

        /// <summary>
        ///     Creates PullingConsumer
        /// </summary>
        /// <param name="options">The options</param>
        /// <param name="queue">The queue</param>
        /// <param name="channel">The channel</param>
        /// <param name="interceptor">The produce-consumer interceptor</param>
        public PullingConsumer(
            PullingConsumerOptions options,
            IQueue queue,
            IPersistentChannel channel,
            IProduceConsumeInterceptor interceptor
        )
        {
            this.queue = queue;
            this.options = options;
            this.channel = channel;
            this.interceptor = interceptor;
        }

        /// <inheritdoc />
        public async Task<PullResult> PullAsync(CancellationToken cancellationToken = default)
        {
            using var cts = cancellationToken.WithTimeout(options.Timeout);

            var basicGetResult = await channel.InvokeChannelActionAsync(
                x => x.BasicGet(queue.Name, options.AutoAck), cts.Token
            ).ConfigureAwait(false);

            if (basicGetResult == null)
                return PullResult.NotAvailable;

            var messagesCount = basicGetResult.MessageCount;
            var message = new ConsumedMessage(
                new MessageReceivedInfo(
                    "",
                    basicGetResult.DeliveryTag,
                    basicGetResult.Redelivered,
                    basicGetResult.Exchange,
                    basicGetResult.RoutingKey,
                    queue.Name
                ),
                new MessageProperties(basicGetResult.BasicProperties),
                basicGetResult.Body.ToArray()
            );
            var interceptedMessage = interceptor.OnConsume(message);
            return PullResult.Available(
                messagesCount, interceptedMessage.ReceivedInfo, interceptedMessage.Properties, interceptedMessage.Body
            );
        }

        /// <inheritdoc />
        public async Task AckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default)
        {
            if (options.AutoAck)
                throw new InvalidOperationException("Cannot ack in auto ack mode");

            using var cts = cancellationToken.WithTimeout(options.Timeout);

            await channel.InvokeChannelActionAsync(
                x => x.BasicAck(deliveryTag, multiple), cts.Token
            ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task RejectAsync(
            ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default
        )
        {
            if (options.AutoAck)
                throw new InvalidOperationException("Cannot reject in auto ack mode");

            using var cts = cancellationToken.WithTimeout(options.Timeout);

            await channel.InvokeChannelActionAsync(
                x => x.BasicNack(deliveryTag, multiple, requeue), cts.Token
            ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            channel.Dispose();
        }
    }

    /// <inheritdoc />
    public class PullingConsumer<T> : IPullingConsumer<PullResult<T>>
    {
        private readonly IPullingConsumer<PullResult> consumer;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;

        /// <summary>
        ///     Creates PullingConsumer
        /// </summary>
        public PullingConsumer(
            IPullingConsumer<PullResult> consumer, IMessageSerializationStrategy messageSerializationStrategy
        )
        {
            this.consumer = consumer;
            this.messageSerializationStrategy = messageSerializationStrategy;
        }

        /// <inheritdoc />
        public async Task<PullResult<T>> PullAsync(CancellationToken cancellationToken = default)
        {
            var pullResult = await consumer.PullAsync(cancellationToken).ConfigureAwait(false);
            if (!pullResult.IsAvailable)
                return PullResult<T>.NotAvailable;

            var message = messageSerializationStrategy.DeserializeMessage(pullResult.Properties, pullResult.Body);
            if (typeof(T).IsAssignableFrom(message.MessageType))
                return PullResult<T>.Available(
                    pullResult.MessagesCount,
                    pullResult.ReceivedInfo,
                    new Message<T>((T) message.GetBody(), message.Properties)
                );

            throw new EasyNetQException(
                $"Incorrect message type returned. Expected {typeof(T).Name}, but was {message.MessageType.Name}"
            );
        }

        /// <inheritdoc />
        public Task AckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken = default)
        {
            return consumer.AckAsync(deliveryTag, multiple, cancellationToken);
        }

        /// <inheritdoc />
        public Task RejectAsync(
            ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken = default
        )
        {
            return consumer.RejectAsync(deliveryTag, multiple, requeue, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            consumer.Dispose();
        }
    }
}
