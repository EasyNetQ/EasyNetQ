using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class DefaultSendReceive : ISendReceive
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        
        private readonly ConcurrentDictionary<string, IQueue> declaredQueues = new ConcurrentDictionary<string, IQueue>(); 

        public DefaultSendReceive(
            IAdvancedBus advancedBus,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy
        )
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");

            this.advancedBus = advancedBus;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        }

        public Task SendAsync<T>(string queue, T message, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(message, "message");

            DeclareQueue(queue);

            var wrappedMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T))
                }
            };

            return advancedBus.PublishAsync(Exchange.GetDefault(), queue, false, wrappedMessage, cancellationToken);
        }

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

        private async Task<IDisposable> ReceiveInternalAsync<T>(
            string queue, 
            Func<T, CancellationToken, Task> onMessage,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            var declaredQueue = DeclareQueue(queue);
            return advancedBus.Consume<T>(declaredQueue, (message, info) => onMessage(message.Body, default), configure);
        }

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

        private async Task<IDisposable> ReceiveInternalAsync(
            string queue, 
            Action<IReceiveRegistration> addHandlers,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            var declaredQueue = DeclareQueue(queue);
            return advancedBus.Consume(declaredQueue, x => addHandlers(new HandlerAdder(x)), configure);
        }
        
        private IQueue DeclareQueue(string queueName)
        {
            IQueue queue = null;
            declaredQueues.AddOrUpdate(
                queueName, 
                key => queue = advancedBus.QueueDeclare(queueName), 
                (key, value) => queue = value);

            return queue;
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
                handlerRegistration.Add<T>((message, info) => onMessage(message.Body, default));
                return this;
            }
        }
    }
}