using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    /// <summary>
    ///     The result of a pull batch operation
    /// </summary>
    public readonly struct PullBatchResult<TPullResult> where TPullResult : IPullResult
    {
        /// <summary>
        ///     Creates PullBatchResult
        /// </summary>
        /// <param name="messages">The messages</param>
        public PullBatchResult(IReadOnlyList<TPullResult> messages)
        {
            Messages = messages;
        }

        /// <summary>
        ///     Messages of the batch
        /// </summary>
        public IReadOnlyList<TPullResult> Messages { get; }

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
        public static Task AckAsync<TPullResult>(
            this IPullingConsumer<TPullResult> consumer,
            ulong deliveryTag,
            CancellationToken cancellationToken = default
        ) where TPullResult : IPullResult
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
        public static Task RejectAsync<TPullResult>(
            this IPullingConsumer<TPullResult> consumer,
            ulong deliveryTag,
            bool requeue = false,
            CancellationToken cancellationToken = default
        ) where TPullResult : IPullResult
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
        public static async Task<PullBatchResult<TPullResult>> PullBatchAsync<TPullResult>(
            this IPullingConsumer<TPullResult> consumer, int batchSize, CancellationToken cancellationToken = default
        ) where TPullResult : IPullResult
        {
            Preconditions.CheckNotNull(consumer, "consumer");

            var messages = new List<TPullResult>(batchSize);
            for (var i = 0; i < batchSize; ++i)
            {
                var pullResult = await consumer.PullAsync(cancellationToken).ConfigureAwait(false);
                if (!pullResult.IsAvailable)
                    break;

                messages.Add(pullResult);
            }

            return new PullBatchResult<TPullResult>(messages);
        }

        /// <summary>
        ///     Acknowledges all messages of the batch
        /// </summary>
        /// <param name="consumer">The consumer</param>
        /// <param name="deliveryTag">The delivery tag of the batch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task AckBatchAsync<TPullResult>(
            this IPullingConsumer<TPullResult> consumer,
            ulong deliveryTag,
            CancellationToken cancellationToken = default
        ) where TPullResult : IPullResult
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
        public static Task RejectBatchAsync<TPullResult>(
            this IPullingConsumer<TPullResult> consumer,
            ulong deliveryTag,
            bool requeue = false,
            CancellationToken cancellationToken = default
        ) where TPullResult : IPullResult
        {
            Preconditions.CheckNotNull(consumer, "consumer");

            return consumer.RejectAsync(deliveryTag, true, requeue, cancellationToken);
        }
    }
}
