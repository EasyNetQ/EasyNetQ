using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Pending confirmation which could be waited for ack, nack and etc.
    /// </summary>
    public interface IPublishPendingConfirmation
    {
        /// <summary>
        /// Wait confirmation for ack, nack and etc.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        Task WaitAsync(CancellationToken cancellationToken);
    }
}
