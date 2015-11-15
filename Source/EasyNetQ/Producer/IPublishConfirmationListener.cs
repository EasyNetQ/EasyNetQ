using System;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public interface IPublishConfirmationListener : IDisposable
    {
        IPublishConfirmationWaiter GetWaiter(IModel model);
    }
}