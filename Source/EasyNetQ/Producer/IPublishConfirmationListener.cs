using System;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    public interface IPublishConfirmationListener : IDisposable
    {
        void Request(ulong deliveryTag);
        void Discard(ulong deliveryTag);
        void Wait(ulong sequenceNumber, TimeSpan timeout);
        Task WaitAsync(ulong sequenceNumber, TimeSpan timeout);
    }
}