using System;

namespace EasyNetQ.Producer
{
    public interface IPublishConfirmationListener : IDisposable
    {
        IPublishConfirmationWaiter GetWaiter(ulong deliveryTag);
    }
}