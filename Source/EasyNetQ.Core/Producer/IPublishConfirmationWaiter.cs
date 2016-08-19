using System;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    public interface IPublishConfirmationWaiter
    {
        void Wait(TimeSpan timeout);
        Task WaitAsync(TimeSpan timeout);
        void Cancel();
    }
}