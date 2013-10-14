using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public interface IPublisherConfirms
    {
        Task PublishWithConfirm(IModel model, Action<IModel> publishAction);
    }
}