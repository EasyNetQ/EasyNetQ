using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    /// <summary>
    ///     The item of the messages batch
    /// </summary>
    public readonly struct PullBatchItem
    {
        /// <summary>
        ///     Creates PullBatchItem
        /// </summary>
        /// <param name="receivedInfo">The received info</param>
        /// <param name="properties">The properties</param>
        /// <param name="body">The body</param>
        public PullBatchItem(MessageReceivedInfo receivedInfo, MessageProperties properties, byte[] body)
        {
            ReceivedInfo = receivedInfo;
            Properties = properties;
            Body = body;
        }

        /// <summary>
        ///     A received info associated with the message
        /// </summary>
        public MessageReceivedInfo ReceivedInfo { get; }

        /// <summary>
        ///     Various message properties
        /// </summary>
        public MessageProperties Properties { get; }

        /// <summary>
        ///     The message body
        /// </summary>
        public byte[] Body { get; }
    }

    /// <summary>
    ///     The result of a pull batch operation
    /// </summary>
    public readonly struct PullBatchResult
    {
        /// <summary>
        ///     Creates PullBatchResult
        /// </summary>
        /// <param name="messages">The messages</param>
        public PullBatchResult(IReadOnlyList<PullBatchItem> messages)
        {
            Messages = messages;
        }

        /// <summary>
        ///     Messages of the batch
        /// </summary>
        public IReadOnlyList<PullBatchItem> Messages { get; }

        /// <summary>
        ///     Returns delivery tag of the batch
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public ulong DeliveryTag
        {
            get
            {
                if (Messages.Count == 0)
                    throw new InvalidOperationException("No messages");

                return Messages.Max(x => x.ReceivedInfo.DeliveryTag);
            }
        }
    }

    /// <summary>
    ///     Various extensions for IPullingConsumer
    /// </summary>
    public static class PullingConsumerExtensions
    {
        /// <summary>
        ///     Acknowledges single message
        /// </summary>
        /// <param name="consumer">The consumer</param>
        /// <param name="deliveryTag">The delivery tag</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task AckAsync(
            this IPullingConsumer consumer, ulong deliveryTag, CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(consumer, "consumer");

            return consumer.AckAsync(deliveryTag, false, cancellationToken);
        }

        /// <summary>
        ///     Rejects single message
        /// </summary>
        /// <param name="consumer">The consumer</param>
        /// <param name="deliveryTag">The delivery tag</param>
        /// <param name="requeue">True if the message should be returned to the queue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task RejectAsync(
            this IPullingConsumer consumer, ulong deliveryTag, bool requeue = false,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(consumer, "consumer");

            return consumer.RejectAsync(deliveryTag, false, requeue, cancellationToken);
        }

        /// <summary>
        ///     Pulls a batch of messages
        /// </summary>
        /// <param name="consumer">The consumer</param>
        /// <param name="batchSize">The size of batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Batch of messages</returns>
        public static async Task<PullBatchResult> PullBatchAsync(
            this IPullingConsumer consumer, int batchSize, CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(consumer, "consumer");

            var messages = new List<PullBatchItem>(batchSize);
            for (var i = 0; i < batchSize; ++i)
            {
                var pullResult = await consumer.PullAsync(cancellationToken).ConfigureAwait(false);
                if (!pullResult.IsAvailable)
                    break;

                messages.Add(new PullBatchItem(pullResult.ReceivedInfo, pullResult.Properties, pullResult.Body));
            }

            return new PullBatchResult(messages);
        }

        /// <summary>
        ///     Acknowledges all messages of the batch
        /// </summary>
        /// <param name="consumer">The consumer</param>
        /// <param name="deliveryTag">The delivery tag of the batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task AckBatchAsync(
            this IPullingConsumer consumer,
            ulong deliveryTag,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(consumer, "consumer");

            return consumer.AckAsync(deliveryTag, true, cancellationToken);
        }

        /// <summary>
        ///     Rejects all messages of the batch
        /// </summary>
        /// <param name="consumer">The consumer</param>
        /// <param name="deliveryTag">The delivery tag of the batch</param>
        /// <param name="requeue">True if all messages of batch should be returned to the queue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task RejectBatchAsync(
            this IPullingConsumer consumer,
            ulong deliveryTag,
            bool requeue = false,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(consumer, "consumer");

            return consumer.RejectAsync(deliveryTag, true, requeue, cancellationToken);
        }
    }
}
