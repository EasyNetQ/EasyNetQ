using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client.Framing;

namespace EasyNetQ.Hosepipe
{
    public class QueueInsertion : IQueueInsertion
    {
        public void PublishMessagesToQueue(IEnumerable<HosepipeMessage> messages, QueueParameters parameters)
        {
            using (var connection = HosepipeConnection.FromParamters(parameters))
            using (var channel = connection.CreateModel())
            {
                foreach (var message in messages)
                {
                    var body = Encoding.UTF8.GetBytes(message.Body);

                    var properties = new BasicProperties();
                    message.Properties.CopyTo(properties);

                    channel.BasicPublish(message.Info.Exchange, message.Info.RoutingKey, properties, body);
                }
            }                        
        }
    }
}