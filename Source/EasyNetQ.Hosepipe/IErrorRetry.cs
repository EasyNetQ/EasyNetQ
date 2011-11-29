using System.Collections.Generic;

namespace EasyNetQ.Hosepipe
{
    public interface IErrorRetry
    {
        void RetryErrors(IEnumerable<string> rawErrorMessages, QueueParameters parameters);
    }
}