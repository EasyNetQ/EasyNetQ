using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     Default implementation of EasyNetQ's send-receive pattern
    /// </summary>
    public class DefaultSendReceive : ISendReceive
    {
        private readonly AsyncLock asyncLock = new AsyncLock();
        private readonly ConcurrentDictionary<string, IQueue> declaredQueues = new ConcurrentDictionary<string, IQueue>();

        private readonly ConnectionConfiguration configuration;
        private readonly IAdvancedBus advancedBus;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        public DefaultSendReceive(
            ConnectionConfiguration configuration,
            IAdvancedBus advancedBus,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");

            this.configuration = configuration;
            this.advancedBus = advancedBus;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        }

        /// <inheritdoc />
        public async Task SendAsync<T>(string queue, T message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(message, "message");

            using var cts = CreateCancellationTokenSource(cancellationToken);

            await DeclareQueueAsync(queue, cts.Token).ConfigureAwait(false);

            var wrappedMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T))
                }
            };

            await advancedBus.PublishAsync(Exchange.GetDefault(), queue, false, wrappedMessage, cts.Token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public AwaitableDisposable<IDisposable> ReceiveAsync<T>(
            string queue,
            Func<T, CancellationToken, Task> onMessage,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return ReceiveInternalAsync(queue, onMessage, configure, cancellationToken).ToAwaitableDisposable();
        }

        /// <inheritdoc />
        public AwaitableDisposable<IDisposable> ReceiveAsync(
            string queue,
            Action<IReceiveRegistration> addHandlers,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(addHandlers, "addHandlers");
            Preconditions.CheckNotNull(configure, "configure");

            return ReceiveInternalAsync(queue, addHandlers, configure, cancellationToken).ToAwaitableDisposable();
        }

        private async Task<IDisposable> ReceiveInternalAsync<T>(
            string queue,
            Func<T, CancellationToken, Task> onMessage,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            using var cts = CreateCancellationTokenSource(cancellationToken);

            var declaredQueue = await DeclareQueueAsync(queue, cts.Token).ConfigureAwait(false);
            return advancedBus.Consume<T>(declaredQueue, (message, info) => onMessage(message.Body, default), configure);
        }

        private async Task<IDisposable> ReceiveInternalAsync(
            string queue,
            Action<IReceiveRegistration> addHandlers,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            using var cts = CreateCancellationTokenSource(cancellationToken);

            var declaredQueue = await DeclareQueueAsync(queue, cts.Token).ConfigureAwait(false);
            return advancedBus.Consume(declaredQueue, x => addHandlers(new HandlerAdder(x)), configure);
        }

        private async Task<IQueue> DeclareQueueAsync(string queueName, CancellationToken cancellationToken)
        {
            if (declaredQueues.TryGetValue(queueName, out var queue)) return queue;

            using (await asyncLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                if (declaredQueues.TryGetValue(queueName, out queue)) return queue;

                queue = await advancedBus.QueueDeclareAsync(queueName, cancellationToken).ConfigureAwait(false);
                declaredQueues[queueName] = queue;
                return queue;
            }
        }

        private CancellationTokenSource CreateCancellationTokenSource(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (configuration.Timeout != Timeout.InfiniteTimeSpan)
                cts.CancelAfter(configuration.Timeout);
            return cts;
        }

        private sealed class HandlerAdder : IReceiveRegistration
        {
            private readonly IHandlerRegistration handlerRegistration;

            public HandlerAdder(IHandlerRegistration handlerRegistration)
            {
                this.handlerRegistration = handlerRegistration;
            }

            public IReceiveRegistration Add<T>(Func<T, CancellationToken, Task> onMessage)
            {
                handlerRegistration.Add<T>((message, info, c) => onMessage(message.Body, c));
                return this;
            }
        }
    }
}
