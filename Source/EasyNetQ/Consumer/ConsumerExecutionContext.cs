using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Consumer
{
    public class ConsumerExecutionContext
    {
        public Func<byte[], MessageProperties, MessageReceivedInfo, Task> UserHandler { get; private set; }
        public MessageReceivedInfo Info { get; private set; }
        public MessageProperties Properties { get; private set; }
        public byte[] Body { get; private set; }
        public IBasicConsumer Consumer { get; private set; }

        public ConsumerExecutionContext(
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> userHandler, 
            MessageReceivedInfo info, 
            MessageProperties properties, 
            byte[] body, 
            IBasicConsumer consumer)
        {
            Preconditions.CheckNotNull(userHandler, "userHandler");
            Preconditions.CheckNotNull(info, "info");
            Preconditions.CheckNotNull(properties, "properties");
            Preconditions.CheckNotNull(body, "body");
            Preconditions.CheckNotNull(consumer, "consumer");

            UserHandler = userHandler;
            Info = info;
            Properties = properties;
            Body = body;
            Consumer = consumer;
        }

        private readonly IList<Action> postAckCallbacks = new List<Action>();

        public void SetPostAckCallback(Action callback)
        {
            postAckCallbacks.Add(callback);
        }

        public IEnumerable<Action> PostAckCallbacks { get { return postAckCallbacks; } } 
    }
}