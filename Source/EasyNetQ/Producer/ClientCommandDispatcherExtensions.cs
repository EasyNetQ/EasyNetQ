using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    internal static class ClientCommandDispatcherExtensions
    {
        public static Task<T> InvokeAsync<T>(
            this IClientCommandDispatcher dispatcher,
            Func<IModel, T> channelAction,
            CancellationToken cancellationToken
        )
        {
            return dispatcher.InvokeAsync(channelAction, ChannelDispatchOptions.Default, cancellationToken);
        }

        public static Task InvokeAsync(
            this IClientCommandDispatcher dispatcher,
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

        public static Task InvokeAsync(
            this IClientCommandDispatcher dispatcher,
            Action<IModel> channelAction,
            CancellationToken cancellationToken
        )
        {
            return dispatcher.InvokeAsync(channelAction, ChannelDispatchOptions.Default, cancellationToken);
        }

        private readonly struct NoContentStruct
        {
        }
    }
}
