using System;
using System.Threading;
using EasyNetQ.Producer;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public static class PersistentChannelExtensions
    {
        public static void InvokeChannelAction(
            this IPersistentChannel source, Action<IModel> channelAction, CancellationToken cancellationToken = default
        )
        {
            source.InvokeChannelActionAsync<NoContentStruct>(model =>
                {
                    channelAction(model);
                    return default;
                }, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        private struct NoContentStruct
        {
        }
    }
}
