using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public class SendReceive : ISendReceive
    {
        private readonly IAdvancedBus advancedBus;

        private readonly ConcurrentDictionary<string, Tuple<IHandlerRegistration, IDisposable>> handlerCollections =
            new ConcurrentDictionary<string, Tuple<IHandlerRegistration, IDisposable>>();

        private readonly ConcurrentDictionary<string, IQueue> declaredQueues = new ConcurrentDictionary<string, IQueue>(); 

        public SendReceive(IAdvancedBus advancedBus)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");

            this.advancedBus = advancedBus;
        }

        public void Send<T>(string queue, T message)
            where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(message, "message");

            DeclareQueue(queue);
            advancedBus.Publish(Exchange.GetDefault(), queue, false, false, new Message<T>(message));
        }

        public IDisposable Receive<T>(string queue, Action<T> onMessage)
            where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            return Receive<T>(queue, message => TaskHelpers.ExecuteSynchronously(() => onMessage(message)));
        }

        public IDisposable Receive<T>(string queue, Func<T, Task> onMessage)
            where T : class
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            IDisposable disposable = null;
            handlerCollections.AddOrUpdate(
                queue,
                key =>
                    {
                        var declaredQueue = DeclareQueue(queue);
                        IHandlerRegistration handlerRegistration = null;
                        disposable = advancedBus.Consume(declaredQueue, registration =>
                            {
                                registration.Add<T>((message, info) => onMessage(message.Body));
                                handlerRegistration = registration;
                            });
                        return new Tuple<IHandlerRegistration, IDisposable>(handlerRegistration, disposable);
                    },
                (key, value) =>
                    {
                        var registration = value.Item1;
                        disposable = value.Item2;
                        registration.Add<T>((message, info) => onMessage(message.Body));
                        return value;
                    });
            return disposable;
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
    }
}