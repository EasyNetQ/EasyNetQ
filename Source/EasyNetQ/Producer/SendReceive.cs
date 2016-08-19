using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class SendReceive : ISendReceive
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        
        private readonly ConcurrentDictionary<string, IQueue> declaredQueues = new ConcurrentDictionary<string, IQueue>(); 

        public SendReceive(
            IAdvancedBus advancedBus,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");

            this.advancedBus = advancedBus;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        }

        public void Send<T>(string queue, T message)
            where T : class
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

            advancedBus.Publish(Exchange.GetDefault(), queue, false, wrappedMessage);
        }

        public Task SendAsync<T>(string queue, T message)
            where T : class
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

            return advancedBus.PublishAsync(Exchange.GetDefault(), queue, false, wrappedMessage);
        }

        public IDisposable Receive<T>(string queue, Action<T> onMessage)
            where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            return Receive<T>(queue, message => TaskHelpers.ExecuteSynchronously(() => onMessage(message)));
        }

        public IDisposable Receive<T>(string queue, Action<T> onMessage, Action<IConsumerConfiguration> configure)
            where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return Receive<T>(queue, message => TaskHelpers.ExecuteSynchronously(() => onMessage(message)), configure);
        }

        public IDisposable Receive<T>(string queue, Func<T, Task> onMessage)
            where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            var declaredQueue = DeclareQueue(queue);
            return advancedBus.Consume<T>(declaredQueue, (message, info) => onMessage(message.Body));
        }

        public IDisposable Receive<T>(string queue, Func<T, Task> onMessage, Action<IConsumerConfiguration> configure)
            where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            var declaredQueue = DeclareQueue(queue);
            return advancedBus.Consume<T>(declaredQueue, (message, info) => onMessage(message.Body), configure);
        }

        public IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers)
        {
            var declaredQueue = DeclareQueue(queue);
            return advancedBus.Consume(declaredQueue, x => addHandlers(new HandlerAdder(x)));
        }

        public IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers, Action<IConsumerConfiguration> configure)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(addHandlers, "addHandlers");
            Preconditions.CheckNotNull(configure, "configure");

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

        private class HandlerAdder : IReceiveRegistration
        {
            private readonly IHandlerRegistration handlerRegistration;

            public HandlerAdder(IHandlerRegistration handlerRegistration)
            {
                this.handlerRegistration = handlerRegistration;
            }

            public IReceiveRegistration Add<T>(Func<T, Task> onMessage) where T : class
            {
                handlerRegistration.Add<T>((message, info) => onMessage(message.Body));
                return this;
            }

            public IReceiveRegistration Add<T>(Action<T> onMessage) where T : class
            {
                handlerRegistration.Add<T>((message, info) => onMessage(message.Body));
                return this;
            }
        }
    }
}