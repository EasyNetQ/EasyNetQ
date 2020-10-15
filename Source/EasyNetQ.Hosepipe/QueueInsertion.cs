using System;
using System.Collections.Generic;

using EasyNetQ.Consumer;

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
            using var connection = HosepipeConnection.FromParameters(parameters);
            using var channel = connection.CreateModel();

            channel.ConfirmSelect();

            foreach (var message in messages)
            {
                var body = errorMessageSerializer.Deserialize(message.Body);

                var properties = channel.CreateBasicProperties();
                message.Properties.CopyTo(properties);

                var queueName = string.IsNullOrEmpty(parameters.QueueName)
                    ? message.Info.Queue
                    : parameters.QueueName;
                channel.BasicPublish("", queueName, true, properties, body);

                channel.WaitForConfirmsOrDie(parameters.ConfirmsTimeout);
            }
        }
    }
}
