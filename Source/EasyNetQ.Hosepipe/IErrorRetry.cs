using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace EasyNetQ.Hosepipe;

public interface IErrorRetry
{
    Task RetryErrorsAsync(IAsyncEnumerable<HosepipeMessage> rawErrorMessages, QueueParameters parameters, CancellationToken cancellationToken = default);
}
