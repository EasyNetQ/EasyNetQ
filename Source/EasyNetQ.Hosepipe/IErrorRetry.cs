using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Hosepipe;

public interface IErrorRetry
{
    Task RetryErrorsAsync(IAsyncEnumerable<HosepipeMessage> rawErrorMessages, QueueParameters parameters, CancellationToken cancellationToken = default);
}
