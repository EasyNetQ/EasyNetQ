using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mike.AmqpSpike
{
    public class PingPongSpike
    {
        const string queueA = "queueA";
        const string queueB = "queueB";

        public void DoBasicGetPingPong()
        {
            IPinger pingerA = new BasicGetPinger();
            IPinger pingerB = new BasicGetPinger();

            DoPingPong(pingerB, pingerA);
        }

        public void DoQueueingBasicConsumerPingPong()
        {
            IPinger pingerA = new QueueingBasicConsumerPinger();
            IPinger pingerB = new QueueingBasicConsumerPinger();

            DoPingPong(pingerB, pingerA);
        }

        public void Clear()
        {
            var pingerA = new BasicGetPinger();
            var pingerB = new BasicGetPinger();

            pingerA.Subscribe(queueA, Console.WriteLine);
            pingerB.Subscribe(queueB, Console.WriteLine);

            Thread.Sleep(100);
        }

        private static void DoPingPong(IPinger pingerB, IPinger pingerA)
        {
            pingerA.Subscribe(queueA, message =>
            {
                Console.WriteLine("A {0}", message);
                pingerA.Publish(queueB, (int.Parse(message) + 1).ToString());
            });

            pingerB.Subscribe(queueB, message =>
            {
                Console.WriteLine("B {0}", message);
                pingerB.Publish(queueA, (int.Parse(message) + 1).ToString());
            });

            pingerA.Publish(queueB, 0.ToString());

            Thread.Sleep(1000);

            pingerA.Dispose();
            pingerB.Dispose();
        }
    }

    public interface IPinger : IDisposable
    {
        void Subscribe(string queue, Action<string> subscribeAction);
        void Publish(string queue, string message);
    }

    public abstract class PingerBase : IPinger
    {
        protected readonly IConnection Connection;
        protected static readonly ConcurrentBag<Action> Actions = new ConcurrentBag<Action>();

        static PingerBase()
        {
            var consumerThread = new Thread(_ =>
            {
                while (true)
                {
                    foreach (var action in Actions)
                    {
                        action();
                    }
                }
            });
            consumerThread.Start();
        }

        protected PingerBase()
        {
            var connectionFactory = new ConnectionFactory();
            Connection = connectionFactory.CreateConnection();
        }

        protected bool Disposed;
        public void Dispose()
        {
            if (Disposed) return;

            Connection.Close();
            Disposed = true;
        }

        public abstract void Subscribe(string queue, Action<string> subscribeAction);

        public void Publish(string queue, string message)
        {
            if (Disposed) return;

            try
            {
                var channel = Connection.CreateModel();
                var properties = channel.CreateBasicProperties();
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish("", queue, properties, body);
            }
            catch (OperationInterruptedException exception)
            {
                // just end without doing anything
            }
        }
    }

    public class BasicGetPinger : PingerBase
    {
        public override void Subscribe(string queue, Action<string> subscribeAction)
        {
            var channel = Connection.CreateModel();
            channel.QueueDeclare(queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Action subscription = () =>
            {
                try
                {
                    var basicGetResult = channel.BasicGet(queue, true);

                    // basicGetResult is often null if no message is available
                    if (basicGetResult != null)
                    {
                        var message = Encoding.UTF8.GetString(basicGetResult.Body);
                        subscribeAction(message);
                    }
                }
                catch (OperationInterruptedException) {}
            };
            Actions.Add(subscription);
        }
    }

    public class QueueingBasicConsumerPinger : PingerBase
    {
        public override void Subscribe(string queue, Action<string> subscribeAction)
        {
            var channel = Connection.CreateModel();
            channel.QueueDeclare(queue,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            var consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(queue, false, consumer);

            Action subscription = () =>
            {
                try
                {
                    var basicDeliverEventArgs = (BasicDeliverEventArgs) consumer.Queue.DequeueNoWait(null);
                    if (basicDeliverEventArgs != null)
                    {
                        var message = Encoding.UTF8.GetString(basicDeliverEventArgs.Body);
                        subscribeAction(message);

                        //consumer.Model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
                        consumer.Model.BasicNack(basicDeliverEventArgs.DeliveryTag, false, true);
                    }
                }
                catch (EndOfStreamException) {}
            };
            Actions.Add(subscription);
        }
    }
}