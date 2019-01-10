using System;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    public interface IPublishConfirmationWaiter
    {
        Task WaitAsync(TimeSpan timeout);
        void Cancel();
    }
}