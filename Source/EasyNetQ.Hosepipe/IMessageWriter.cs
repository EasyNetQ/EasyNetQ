using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace EasyNetQ.Hosepipe;

public interface IMessageWriter
{
    Task WriteAsync(IAsyncEnumerable<HosepipeMessage> messages, QueueParameters parameters, CancellationToken cancellationToken = default);
}
