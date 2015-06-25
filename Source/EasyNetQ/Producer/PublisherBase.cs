using System;
using System.Threading.Tasks;
using EasyNetQ.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Producer
{
    public abstract class PublisherBase : IPublisher
    {
        private readonly IEventBus eventBus;
        private IModel cachedModel;

        protected PublisherBase(IEventBus eventBus)
        {
            Preconditions.CheckNotNull(eventBus, "eventBus");
            
            this.eventBus = eventBus;
        }

        protected void SetModel(IModel model)
        {
            // we only need to set up the channel once, but the persistent channel can change
            // the IModel instance underneath us, so check on each publish.
            if (cachedModel == model) return;

            if (cachedModel != null)
            {
                OnChannelClosed(cachedModel);
            }

            cachedModel = model;

            OnChannelOpened(model);
        }

        protected virtual void OnChannelOpened(IModel newModel)
        {
            newModel.BasicReturn += ModelOnBasicReturn;
        }

        protected virtual void OnChannelClosed(IModel oldModel)
        {
            oldModel.BasicReturn -= ModelOnBasicReturn;
        }

        public abstract Task PublishAsync(IModel model, Action<IModel> publishAction);

        protected void ModelOnBasicReturn(object model, BasicReturnEventArgs e)
        {
            eventBus.Publish(new ReturnedMessageEvent(e.Body,
                new MessageProperties(e.BasicProperties),
                new MessageReturnedInfo(e.Exchange, e.RoutingKey, e.ReplyText)));
        }

        protected struct NullStruct { }
    }
}