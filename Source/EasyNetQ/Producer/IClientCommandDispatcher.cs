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
        Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken = default);
        Task InvokeAsync(Action<IModel> channelAction, CancellationToken cancellationToken = default);
    }
}
