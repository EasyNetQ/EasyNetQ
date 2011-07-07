using System;
using System.IO;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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
            const int prefetchCount = 0;
            const bool noAck = false;
            const int numberOfMessagesToConsume = 10;

            WithChannel.Do(channel =>
            {
                channel.BasicQos(0, prefetchCount, false);

                var consumer = new QueueingBasicConsumer(channel);
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

                                consumer.Model.BasicAck(basicDeliverEventArgs.DeliveryTag, false);
                                //consumer.Model.BasicNack(basicDeliverEventArgs.DeliveryTag, false, requeue:true);
                            }
                        }
                        catch (EndOfStreamException exception)
                        {
                            break;
                        }
                    }

                });
                thread.Start();

                Thread.Sleep(1000);
                running = false;
            });
        }
    }
}