using System;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    public interface IPublishConfirmationListener : IDisposable
    {
        void Request(ulong deliveryTag);
        void Cancel(ulong deliveryTag);
        void Wait(ulong deliveryTag, TimeSpan timeout);
        Task WaitAsync(ulong deliveryTag, TimeSpan timeout);
    }
}