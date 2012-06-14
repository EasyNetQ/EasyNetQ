using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Util;

namespace EasyNetQ
{
    /// <summary>
    /// Identical to the C# client's DefaultQueueingConsumer but it doesn't close the queue
    /// when its own model is closed.
    /// </summary>
    public class EasyNetQConsumer : DefaultBasicConsumer
    {
        private readonly SharedQueue queue;
        private bool localModelClosing = false;

        public SharedQueue Queue
        {
            get { return queue; }
        }

        public EasyNetQConsumer(IModel model, SharedQueue queue) : base(model)
        {
            this.queue = queue;
        }

        /// <summary>
        /// Closes the consumer's model without closing the shared queue
        /// </summary>
        public void CloseModel()
        {
            localModelClosing = true;
            try
            {
                this.Model.Close();
            }
            finally
            {
                localModelClosing = false;
            }
        }

        public override void OnCancel()
        {
            if(!localModelClosing)
            {
                queue.Close();
            }
            base.OnCancel();
        }

        /// <summary>
        /// Overrides DefaultBasicConsumer's
        ///             HandleBasicDeliver implementation, building a
        ///             BasicDeliverEventArgs instance and placing it in the
        ///             Queue.
        /// </summary>
        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            queue.Enqueue(new BasicDeliverEventArgs()
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