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
    ///     Represents a result of a message pull
    /// </summary>
    public readonly struct PullResult
    {
        private readonly ConsumedMessage? message;

        /// <summary>
        ///     Represents a result when no messages are available
        /// </summary>
        public static PullResult NotAvailable { get; } = new PullResult(null);

        /// <summary>
        ///     Represents a result when a message is available
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static PullResult Available(ConsumedMessage message)
        {
            return new PullResult(message);
        }

        private PullResult(ConsumedMessage? message)
        {
            this.message = message;
        }

        /// <summary>
        ///     True is a message is available
        /// </summary>
        public bool MessageAvailable => message.HasValue;

        /// <summary>
        ///     Returns a message if it's available
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public ConsumedMessage Message
        {
            get
            {
                if (!message.HasValue)
                    throw new InvalidOperationException("No message is available");

                return message.Value;
            }
        }
    }

    /// <summary>
    ///     Allows to receive messages by pulling them one by one
    /// </summary>
    public interface IPullingConsumer : IDisposable
    {
        /// <summary>
        ///     Receives a single message
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        Task<PullResult> PullAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Acknowledges one or more messages
        /// </summary>
        /// <param name="deliveryTag"></param>
        /// <param name="multiple"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AckAsync(ulong deliveryTag, bool multiple = true, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Rejects one or more messages
        /// </summary>
        /// <param name="deliveryTag"></param>
        /// <param name="multiple"></param>
        /// <param name="requeue"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RejectAsync(
            ulong deliveryTag, bool multiple = true, bool requeue = false, CancellationToken cancellationToken = default
        );
    }


    /// <summary>
    ///     Represent pulling consumer options
    /// </summary>
    public readonly struct PullingConsumerOptions
    {
        public bool AutoAck { get; }

        public TimeSpan Timeout { get; }

        public PullingConsumerOptions(bool autoAck, TimeSpan timeout)
        {
            AutoAck = autoAck;
            Timeout = timeout;
        }
    }

    /// <inheritdoc />
    public class PullingConsumer : IPullingConsumer
    {
        private readonly IPersistentChannel channel;
        private readonly IProduceConsumeInterceptor interceptor;
        private readonly PullingConsumerOptions options;
        private readonly IQueue queue;

        /// <summary>
        ///     Creates PullingConsumer
        /// </summary>
        /// <param name="queue">The queue</param>
        /// <param name="options">The options</param>
        /// <param name="channel">The channel</param>
        /// <param name="interceptor">The produce-consumer interceptor</param>
        public PullingConsumer(
            IQueue queue,
            PullingConsumerOptions options,
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
            return PullResult.Available(interceptor.OnConsume(message));
        }

        /// <inheritdoc />
        public async Task AckAsync(
            ulong deliveryTag, bool multiple = false, CancellationToken cancellationToken = default
        )
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
            ulong deliveryTag, bool multiple = false, bool requeue = false,
            CancellationToken cancellationToken = default
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
        public void Dispose() => channel.Dispose();
    }
}
