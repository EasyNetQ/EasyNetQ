using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer
{
    public interface IConsumeSingle
    {
        /// <summary>
        /// Consume a single message from a queue. Note that it uses no-ack mode. If more messages are in this queue, they might get lost.
        /// </summary>
        /// <returns>Disposable. Call dispose to cancel</returns>
        Task<MessageConsumeContext> ConsumeSingle(IQueue queue, TimeSpan timeout);
    }

    public class DefaultConsumeSingle : IConsumeSingle
    {
        private readonly IClientCommandDispatcher commandDispatcher;
        private bool disposed = false;

        public DefaultConsumeSingle(IClientCommandDispatcherFactory commandDispatcherFactory, IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(commandDispatcherFactory, "commandDispatcherFactory");

            commandDispatcher = commandDispatcherFactory.GetClientCommandDispatcher(connection);
            //commandDispatcher.Invoke(model => model.BasicQos(0, 1, false));
        }

        public Task<MessageConsumeContext> ConsumeSingle(IQueue queue, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<MessageConsumeContext>();
            var consumerTag = Guid.NewGuid().ToString();
            var consumer = new ConsumeSingleConsumer(queue.Name, commandDispatcher, tcs, timeout);
            var arguments = new Dictionary<string, object>();

            commandDispatcher.Invoke(model => model.BasicConsume(queue.Name, true, consumerTag, true, true, arguments, consumer));

            return tcs.Task;
        }

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            commandDispatcher.Dispose();
        }
    }

    public class ConsumeSingleConsumer : IBasicConsumer
    {
        public IModel Model {
            get { throw new Exception("This consumer is single purpose and the model should not be accessed from outside the class"); }
        }

        public event ConsumerCancelledEventHandler ConsumerCancelled;

        private readonly string queueName;
        private readonly IClientCommandDispatcher commandDispatcher;
        private readonly TaskCompletionSource<MessageConsumeContext> tcs;

        private readonly object handledLock = new object();
        private readonly Timer _timer;

        private volatile bool handled = false;
        

        public ConsumeSingleConsumer(string queueName, IClientCommandDispatcher commandDispatcher, TaskCompletionSource<MessageConsumeContext> tcs, TimeSpan timeout)
        {
            this.queueName = queueName;
            this.commandDispatcher = commandDispatcher;
            this.tcs = tcs;
            _timer = new Timer(state =>
                {
                    _timer.Dispose();
                    tcs.TrySetException(new TimeoutException(
                                            string.Format("Consume timed out. QueueName: {0}", queueName)));
                });
            _timer.Change(timeout, TimeSpan.FromMilliseconds(-1));
        }

        public void HandleBasicConsumeOk(string consumerTag)
        {
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
        }

        public void HandleBasicCancel(string consumerTag)
        {
            Cancel();
        }

        public void HandleModelShutdown(IModel model, ShutdownEventArgs reason)
        {
            Cancel();
        }

        private void Cancel()
        {
            if (handled) return;
            lock (handledLock)
            {
                if (handled) return;
                tcs.SetCanceled();
                _timer.Dispose();
            }
        }

        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
            IBasicProperties properties, byte[] body)
        {
            if (handled) return;
            lock (handledLock)
            {
                if (handled) return;
                commandDispatcher.Invoke(model =>
                {
                    tcs.SetResult(new MessageConsumeContext(
                        body,
                        new MessageProperties(properties),
                        new MessageReceivedInfo(consumerTag, deliveryTag, redelivered, exchange, routingKey, queueName)));
                    _timer.Dispose();

                    model.BasicAck(deliveryTag, false);
                    model.BasicCancel(consumerTag);
                    //model.Dispose();
                });
                handled = true;
            }
        }
    }
}