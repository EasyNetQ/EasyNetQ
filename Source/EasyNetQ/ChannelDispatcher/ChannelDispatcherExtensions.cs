using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.ChannelDispatcher
{
    internal static class ChannelDispatcherExtensions
    {
        public static Task InvokeAsync(
            this IChannelDispatcher dispatcher,
            Action<IModel> channelAction,
            ChannelDispatchOptions channelOptions,
            CancellationToken cancellationToken
        )
        {
            return dispatcher.InvokeAsync<NoContentStruct>(model =>
            {
                channelAction(model);
                return default;
            }, channelOptions, cancellationToken);
        }

        private readonly struct NoContentStruct
        {
        }
    }
}
