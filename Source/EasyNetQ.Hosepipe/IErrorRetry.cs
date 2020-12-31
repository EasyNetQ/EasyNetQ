using System.Collections.Generic;

namespace EasyNetQ.Hosepipe
{
    public interface IErrorRetry
    {
        void RetryErrors(IEnumerable<HosepipeMessage> rawErrorMessages, QueueParameters parameters);
    }
}
