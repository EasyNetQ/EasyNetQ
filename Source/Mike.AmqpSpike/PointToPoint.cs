using System;
using System.Text;

namespace Mike.AmqpSpike
{
    public class PointToPoint
    {
        private const string defaultExchange = "";
        private const string queueName = "mike.queue.1";

        public void CreateQueue()
        {
            WithChannel.Do(channel =>
            {
                var returnedName = channel.QueueDeclare(queueName, false, false, false, null);
                Console.WriteLine("Declared queue: {0}", returnedName);
            });
        }

        public void BasicPublish()
        {
            WithChannel.Do(channel =>
            {
                var defaultProperties = channel.CreateBasicProperties();
                var messageText = "Hello World " + Guid.NewGuid().ToString().Substring(0, 5);
                var message = Encoding.UTF8.GetBytes(messageText);

                channel.BasicPublish(defaultExchange, queueName, defaultProperties, message);
                Console.WriteLine("Published Message '{0}'", messageText);
            });
        }

        public void BasicGet()
        {
            WithChannel.Do(channel =>
            {
                var result = channel.BasicGet(queueName, true);

                if (result == null)
                {
                    Console.WriteLine("Queue is empty");
                }
                else
                {
                    var messageText = Encoding.UTF8.GetString(result.Body);
                    Console.WriteLine("Got message: {0}", messageText);
                }
            });
        }
    }
}