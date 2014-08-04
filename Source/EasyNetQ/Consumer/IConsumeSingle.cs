using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer
{
    public interface IConsumeSingle
    {
        /// <summary>
        /// Consume a single message from an exclusive single-use queue
        /// </summary>
        /// <returns>A ConsumeSingleResult</returns>
        ConsumeSingleResult ConsumeSingle(IPersistentConnection connection);
    }

    public class DefaultConsumeSingle : IConsumeSingle
    {
        private readonly IClientCommandDispatcherFactory commandDispatcherFactory;

        public DefaultConsumeSingle(IClientCommandDispatcherFactory commandDispatcherFactory)
        {
            Preconditions.CheckNotNull(commandDispatcherFactory, "commandDispatcherFactory");
            this.commandDispatcherFactory = commandDispatcherFactory;
        }

        public ConsumeSingleResult ConsumeSingle(IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");

            var commandDispatcher = commandDispatcherFactory.GetClientCommandDispatcher(connection);

            var task = commandDispatcher.Invoke(model => model.QueueDeclare());
            task.Wait();
            var queueDeclareOk = task.Result;

            var consumerTag = Guid.NewGuid().ToString();
            var consumer = new ConsumeSingleConsumer(queueDeclareOk.QueueName, commandDispatcher);
            var arguments = new Dictionary<string, object>();

            commandDispatcher.Invoke(model =>
                model.BasicConsume(queueDeclareOk.QueueName, false, consumerTag, true, true, arguments, consumer));

            return new ConsumeSingleResult(consumer.Task, new Queue(queueDeclareOk.QueueName, true));
        }
    }

    public class ConsumeSingleConsumer : IBasicConsumer
    {
        public IModel Model {
            get { throw new NotImplementedException(); }
        }
        public event ConsumerCancelledEventHandler ConsumerCancelled;
        public Task<ConsumeSingleMessageContext> Task { get; private set; }

        private readonly string queueName;
        private readonly IClientCommandDispatcher commandDispatcher;

        private readonly TaskCompletionSource<ConsumeSingleMessageContext> tcs = 
            new TaskCompletionSource<ConsumeSingleMessageContext>();

        private bool handled = false;
        private readonly object handledLock = new object();

        public ConsumeSingleConsumer(string queueName, IClientCommandDispatcher commandDispatcher)
        {
            this.queueName = queueName;
            this.commandDispatcher = commandDispatcher;
            Task = tcs.Task;
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
                    tcs.SetResult(new ConsumeSingleMessageContext(
                        body,
                        new MessageProperties(properties),
                        new MessageReceivedInfo(consumerTag, deliveryTag, redelivered, exchange, routingKey, queueName)));

                    model.BasicAck(deliveryTag, false);
                    //model.BasicCancel(consumerTag);
                    model.Dispose();
                });
                handled = true;
            }
        }
    }
}