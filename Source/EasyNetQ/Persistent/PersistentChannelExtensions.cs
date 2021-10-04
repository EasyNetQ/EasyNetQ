using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Persistent
{
    internal static class PersistentChannelExtensions
    {
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
