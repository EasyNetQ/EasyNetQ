using System.Collections.Generic;

using EasyNetQ.Consumer;

using RabbitMQ.Client.Framing;

namespace EasyNetQ.Hosepipe
{
    public class QueueInsertion : IQueueInsertion
    {
        private readonly IErrorMessageSerializer errorMessageSerializer;

        public QueueInsertion(IErrorMessageSerializer errorMessageSerializer)
        {
            this.errorMessageSerializer = errorMessageSerializer;
        }

        public void PublishMessagesToQueue(IEnumerable<HosepipeMessage> messages, QueueParameters parameters)
        {
            using (var connection = HosepipeConnection.FromParameters(parameters))
            using (var channel = connection.CreateModel())
            {
                foreach (var message in messages)
                {
                    var body = errorMessageSerializer.Deserialize(message.Body);

                    var properties = new BasicProperties();
                    message.Properties.CopyTo(properties);

                    channel.BasicPublish(message.Info.Exchange, message.Info.RoutingKey, true, properties, body);
                }
            }                        
        }
    }
}