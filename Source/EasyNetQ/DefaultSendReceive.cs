using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Default implementation of EasyNetQ's send-receive pattern
    /// </summary>
    public class DefaultSendReceive : ISendReceive
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IAdvancedBus advancedBus;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        /// <summary>
        ///     Creates DefaultSendReceive
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="advancedBus">The advanced bus</param>
        /// <param name="messageDeliveryModeStrategy">The message delivery mode strategy</param>
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
        public async Task SendAsync<T>(
            string queue, T message, Action<ISendConfiguration> configure, CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(message, "message");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var sendConfiguration = new SendConfiguration();
            configure(sendConfiguration);

            var properties = new MessageProperties();
            if (sendConfiguration.Priority != null)
                properties.Priority = sendConfiguration.Priority.Value;
            properties.DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T));

            await advancedBus.PublishAsync(
                Exchange.GetDefault(), queue, configuration.MandatoryPublish, new Message<T>(message, properties), cts.Token
            ).ConfigureAwait(false);
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
            using var cts = cancellationToken.WithTimeout(configuration.Timeout);
            var declaredQueue = await advancedBus.QueueDeclareAsync(queue, cts.Token).ConfigureAwait(false);
            return advancedBus.Consume<T>(declaredQueue, (message, info) => onMessage(message.Body, default), configure);
        }

        private async Task<IDisposable> ReceiveInternalAsync(
            string queue,
            Action<IReceiveRegistration> addHandlers,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            using var cts = cancellationToken.WithTimeout(configuration.Timeout);
            var declaredQueue = await advancedBus.QueueDeclareAsync(queue, cts.Token).ConfigureAwait(false);
            return advancedBus.Consume(declaredQueue, x => addHandlers(new HandlerAdder(x)), configure);
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
