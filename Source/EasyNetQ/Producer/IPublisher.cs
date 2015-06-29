using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public interface IPublisher
    {
        Task PublishAsync(IModel model, Action<IModel> publishAction);
        void Publish(IModel model, Action<IModel> publishAction);
    }
}