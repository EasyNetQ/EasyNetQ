using System;
using System.IO;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using RabbitMQ.Util;

namespace Mike.AmqpSpike
{
    public class AckNackSpike
    {
        private const string ackNackQueue = "ackNackQueue";

        public void PublishSequence()
        {
            WithChannel.Do(channel =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var properties = channel.CreateBasicProperties();
                    var body = Encoding.UTF8.GetBytes(i.ToString());
                    channel.BasicPublish("", ackNackQueue, properties, body);
                }
            });
        }

        public void InitializeQueue()
        {
            WithChannel.Do(channel => 
                channel.QueueDeclare(
                    ackNackQueue, 
                    durable: true, 
                    exclusive: false, 
                    autoDelete: false, 
                    arguments: null));
        }

        public void SubscribeAck()
        {
            const int prefetchCount = 1;
            const bool noAck = false;
            const int numberOfMessagesToConsume = 1;

            WithChannel.Do(channel =>
            {
                channel.BasicQos(0, prefetchCount, false);

                var consumer = new LocalQueueingBasicConsumer(channel);
                channel.BasicConsume(ackNackQueue, noAck, consumer);

                var running = true;
                var thread = new Thread(_ =>
                {
                    var count = 0;
                    while (running && count++ < numberOfMessagesToConsume)
                    {
                        try
                        {
                            var basicDeliverEventArgs = (BasicDeliverEventArgs) consumer.Queue.Dequeue();

                            if (basicDeliverEventArgs != null)
                            {
                                var message = Encoding.UTF8.GetString(basicDeliverEventArgs.Body);
                                Console.Out.WriteLine("message = {0}", message);

                                Console.WriteLine("Redelivered: {0}", basicDeliverEventArgs.Redelivered);
                                
                                consumer.Model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
                                // consumer.Model.BasicNack(basicDeliverEventArgs.DeliveryTag, false, requeue:true);
                            }
                        }
                        catch (EndOfStreamException)
                        {
                            break;
                        }
                    }

                });
                thread.Start();

                Thread.Sleep(1000);
                running = false;
                channel.Close();
                consumer.Queue.Close();
            });
        }

        public void SubscribeWithSubscriber()
        {
            WithChannel.Do(channel =>
            {
                var subscription = new Subscription(channel, ackNackQueue);
                foreach (BasicDeliverEventArgs deliverEventArgs in subscription)
                {
                    var message = Encoding.UTF8.GetString(deliverEventArgs.Body);
                    Console.Out.WriteLine("message = {0}", message);

                    subscription.Ack(deliverEventArgs);
                }
            });
        }

    }


    public class LocalQueueingBasicConsumer : DefaultBasicConsumer
    {
        protected SharedQueue m_queue;

        public SharedQueue Queue
        {
            get
            {
                return this.m_queue;
            }
        }

        public LocalQueueingBasicConsumer()
            : this((IModel)null)
        {
        }

        public LocalQueueingBasicConsumer(IModel model)
            : this(model, new SharedQueue())
        {
        }

        public LocalQueueingBasicConsumer(IModel model, SharedQueue queue)
            : base(model)
        {
            this.m_queue = queue;
        }

        public override void OnCancel()
        {
            this.m_queue.Close();
            base.OnCancel();
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            Console.Out.WriteLine("HandleBasicDeliver");
            this.m_queue.Enqueue((object)new BasicDeliverEventArgs()
            {
                ConsumerTag = consumerTag,
                DeliveryTag = deliveryTag,
                Redelivered = redelivered,
                Exchange = exchange,
                RoutingKey = routingKey,
                BasicProperties = properties,
                Body = body
            });
        }
    }

}