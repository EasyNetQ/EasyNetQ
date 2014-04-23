using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public interface IPublisher
    {
        Task Publish(IModel model, Action<IModel> publishAction);
    }
}