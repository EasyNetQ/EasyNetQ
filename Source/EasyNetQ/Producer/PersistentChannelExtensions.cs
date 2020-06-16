using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    internal static class PersistentChannelExtensions
    {
        public static void InvokeChannelAction(
            this IPersistentChannel source, Action<IModel> channelAction, CancellationToken cancellationToken = default
        )
        {
            source.InvokeChannelActionAsync(channelAction, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }


        public static Task InvokeChannelActionAsync(
            this IPersistentChannel source, Action<IModel> channelAction, CancellationToken cancellationToken = default
        )
        {
            return source.InvokeChannelActionAsync<NoContentStruct>(model =>
            {
                channelAction(model);
                return default;
            }, cancellationToken);
        }


        private readonly struct NoContentStruct
        {
        }
    }
}
