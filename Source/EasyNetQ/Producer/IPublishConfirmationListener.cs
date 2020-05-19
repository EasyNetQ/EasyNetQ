using System;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// A listener of publish confirmations
    /// </summary>
    public interface IPublishConfirmationListener : IDisposable
    {
        /// <summary>
        /// Creates pending confirmation for a next publish
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Pending confirmation to wait</returns>
        IPublishPendingConfirmation CreatePendingConfirmation(IModel model);
    }
}
