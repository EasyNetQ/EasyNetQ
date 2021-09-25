using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.ChannelDispatcher
{
    /// <summary>
    ///     Responsible for invoking client commands.
    /// </summary>
    public interface IChannelDispatcher : IDisposable
    {
        /// <summary>
        /// Invokes an action on top of model
        /// </summary>
        /// <param name="channelAction"></param>
        /// <param name="channelOptions"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        Task<T> InvokeAsync<T>(
            Func<IModel, T> channelAction, ChannelDispatchOptions channelOptions, CancellationToken cancellationToken = default
        );
    }
}
