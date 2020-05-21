using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    internal static class ClientCommandDispatcherExtensions
    {
        public static Task InvokeAsync(
            this IClientCommandDispatcher dispatcher, Action<IModel> channelAction, CancellationToken cancellationToken
        )
        {
            return dispatcher.InvokeAsync<NoContentStruct>(model =>
            {
                channelAction(model);
                return default;
            }, cancellationToken);
        }

        private struct NoContentStruct
        {
        }
    }
}
