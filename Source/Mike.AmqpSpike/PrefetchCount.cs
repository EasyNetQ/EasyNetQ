using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;

namespace Mike.AmqpSpike
{
    /// <summary>
    /// An experiment to see how prefectch counts can be used to set priorities
    /// </summary>
    public class PrefetchCount
    {
        private const string high = "high.priority";
        private const string low = "low.priority";
        private const int publishCount = 100;

        public void SetUp()
        {
            WithChannel.Do(channel =>
                {
                    channel.QueueDeclare(high, true, false, false, null);
                    channel.QueueDeclare(low, true, false, false, null);
                });
        }

        public byte[] GetMessage()
        {
            return Encoding.UTF8.GetBytes("A message!");
        }

        public void PublishHigh()
        {
            Publish(high);
        }

        public void PublishLow()
        {
            Publish(low);
        }

        public void Publish(string queue)
        {
            WithChannel.Do(channel =>
                {
                    for (var i = 0; i < publishCount; i++)
                    {
                        channel.BasicPublish("", queue, channel.CreateBasicProperties(), GetMessage());
                    }
                });
        }

        public void ConsumeAll()
        {
            var are = new AutoResetEvent(false);
            var factory = new ConnectionFactory
                {
                    Uri = "amqp://localhost/"
                };
            using (var connection = factory.CreateConnection())
            {
                Consume(connection, are, low, 2);
                Consume(connection, are, high, 4);

                Thread.Sleep(10000);
                are.Set();
                Thread.Sleep(0); // let the channels close
            }
        }

        public void Consume(IConnection connection, AutoResetEvent are, string queue, ushort prefetchCount = 1)
        {
            new Thread(() => 
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.BasicQos(0, prefetchCount, false);
                        channel.BasicConsume(queue, false, new Consumer
                        {
                            Queue = queue,
                            Model = channel,
                            Handler = (q, message) =>
                            {
                                Console.Out.WriteLine("{0} {1}: {2}", 
                                    Thread.CurrentThread.ManagedThreadId, q, message);
                                //Thread.Sleep(100);
                            }
                        });
                        are.WaitOne(1000);
                    }
                }){ Name = "channel thread" }.Start();
        }

        public void Everything()
        {
            PublishHigh();
            PublishLow();
            ConsumeAll();
        }
    }

    public class Consumer : IBasicConsumer
    {
        private bool modelShutdown = false;

        public void HandleBasicConsumeOk(string consumerTag)
        {
            throw new System.NotImplementedException();
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            throw new System.NotImplementedException();
        }

        public void HandleBasicCancel(string consumerTag)
        {
            throw new System.NotImplementedException();
        }

        public void HandleModelShutdown(IModel model, ShutdownEventArgs reason)
        {
            modelShutdown = true;
        }

        public void HandleBasicDeliver(
            string consumerTag, 
            ulong deliveryTag, 
            bool redelivered, 
            string exchange, 
            string routingKey,
            IBasicProperties properties, 
            byte[] body)
        {
            var message = Encoding.UTF8.GetString(body);
            if (Handler != null) Handler(Queue, message);
            if (modelShutdown) return;

            Model.BasicAck(deliveryTag, false);
        }

        public IModel Model { get; set; }
        public string Queue { get; set; }
        public Action<string, string> Handler { get; set; } 
    }
}