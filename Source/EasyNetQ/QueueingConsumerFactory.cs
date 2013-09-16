using System.Collections.Concurrent;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    /// <summary>
    /// This consumer factory starts a thread to handle message delivery from
    /// a shared queue. It returns a QueueingBasicConsumer with a reference to the 
    /// queue.
    /// </summary>
    public class QueueingConsumerFactory : IConsumerFactory
    {
        private readonly IEasyNetQLogger logger;
        private readonly IConsumerDispatcherFactory consumerDispatcherFactory;
        private readonly IHandlerExecutionContext handlerExecutionContext;

        private readonly IDictionary<string, SubscriptionInfo> subscriptions = 
            new ConcurrentDictionary<string, SubscriptionInfo>();
        
        public QueueingConsumerFactory(
            IEasyNetQLogger logger, 
            IConsumerDispatcherFactory consumerDispatcherFactory, 
            IHandlerExecutionContext handlerExecutionContext)
        {
            this.logger = logger;
            this.consumerDispatcherFactory = consumerDispatcherFactory;
            this.handlerExecutionContext = handlerExecutionContext;
        }

        public void HandleMessageDelivery(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var consumerTag = basicDeliverEventArgs.ConsumerTag;
            if (consumerTag == null)
            {
                logger.ErrorWrite("BasicDeliverEventArgs.ConsumerTag is null");
                return;
            }
            if (!subscriptions.ContainsKey(consumerTag))
            {
                logger.ErrorWrite("No subscription for ConsumerTag: {0}", consumerTag);
                return;
            }

            var subscriptionInfo = subscriptions[consumerTag];
            handlerExecutionContext.HandleMessageDelivery(subscriptionInfo, basicDeliverEventArgs);

            if(subscriptionInfo.ModelIsSingleUse)
            {
                subscriptions.Remove(consumerTag);
            }
        }

        public DefaultBasicConsumer CreateConsumer(
            SubscriptionAction subscriptionAction,
            IModel model, 
            bool modelIsSingleUse, 
            MessageCallback callback)
        {
            var dispatcher = consumerDispatcherFactory.GetConsumerDispatcher();

            var consumer = new EasyNetQConsumer(model, 
                deliverEventArgs => 
                    dispatcher.QueueAction(() => 
                        HandleMessageDelivery(deliverEventArgs)))
                {
                    ConsumerTag = subscriptionAction.Id
                };

            if (subscriptions.ContainsKey(consumer.ConsumerTag))
            {
                logger.DebugWrite("Removing existing subscription with ConsumerTag: " + consumer.ConsumerTag);
                subscriptions.Remove(consumer.ConsumerTag);
            }

            subscriptions.Add(consumer.ConsumerTag, new SubscriptionInfo(subscriptionAction, consumer, callback, modelIsSingleUse, model));
            return consumer;
        }

        public void ClearConsumers()
        {
            subscriptions.Clear();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            consumerDispatcherFactory.Dispose();
            handlerExecutionContext.Dispose();

            foreach (var subscriptionInfo in subscriptions)
            {
                subscriptionInfo.Value.Channel.Close();
            }

            disposed = true;
        }
    }

    public class SubscriptionInfo
    {
        public SubscriptionAction SubscriptionAction { get; set; }
        public EasyNetQConsumer Consumer { get; private set; }
        public MessageCallback Callback { get; private set; }
        public bool ModelIsSingleUse { get; private set; }
        public IModel Channel { get; private set; }

        public SubscriptionInfo(SubscriptionAction subscriptionAction, EasyNetQConsumer consumer, MessageCallback callback, bool modelIsSingleUse, IModel channel)
        {
            SubscriptionAction = subscriptionAction;
            Consumer = consumer;
            Callback = callback;
            ModelIsSingleUse = modelIsSingleUse;
            Channel = channel;
        }
    }
}