using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     Responsible for invoking client commands.
    /// </summary>
    public interface IClientCommandDispatcher : IDisposable
    {
        /// <summary>
        /// Invokes a command on top of model
        /// </summary>
        /// <param name="channelAction"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken);
    }
}
