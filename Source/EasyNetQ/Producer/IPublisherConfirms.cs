using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public interface IPublisherConfirms
    {
        Task Publish(IModel model, Action<IModel> publishAction);
    }
}