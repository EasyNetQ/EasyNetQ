using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Consumer
{
    public class ConsumerExecutionContext
    {
        public Func<byte[], MessageProperties, MessageReceivedInfo, Task> UserHandler { get; }
        public MessageReceivedInfo Info { get; }
        public MessageProperties Properties { get; }
        public byte[] Body { get; }

        public ConsumerExecutionContext(
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> userHandler, 
            MessageReceivedInfo info, 
            MessageProperties properties, 
            byte[] body
        )
        {
            Preconditions.CheckNotNull(userHandler, "userHandler");
            Preconditions.CheckNotNull(info, "info");
            Preconditions.CheckNotNull(properties, "properties");
            Preconditions.CheckNotNull(body, "body");

            UserHandler = userHandler;
            Info = info;
            Properties = properties;
            Body = body;
        }
    }
}