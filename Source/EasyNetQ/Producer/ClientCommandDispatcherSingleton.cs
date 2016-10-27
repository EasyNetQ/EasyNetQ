using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public class ClientCommandDispatcherSingleton : IClientCommandDispatcher
    {
        private readonly SemaphoreSlim channelSemaphore;
        private const int queueSize = 1;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly IPersistentChannel persistentChannel;

        public ClientCommandDispatcherSingleton(
            ConnectionConfiguration configuration,
            IPersistentConnection connection,
            IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            persistentChannel = persistentChannelFactory.CreatePersistentChannel(connection);
            channelSemaphore = new SemaphoreSlim(queueSize, queueSize);
        }

        public T Invoke<T>(Func<IModel, T> channelAction)
        {
            try
            {
                return InvokeAsync(channelAction).Result;
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        public void Invoke(Action<IModel> channelAction)
        {
            try
            {
                InvokeAsync(channelAction).Wait();
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        public async Task<T> InvokeAsync<T>(Func<IModel, T> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            await channelSemaphore.WaitAsync(cancellation.Token).ConfigureAwait(false);
            try
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var result = default(T);
                persistentChannel.InvokeChannelAction(channel => { result = channelAction(channel); });
                return result;
            }
            finally
            {
                channelSemaphore.Release();
            }
        }

        public Task InvokeAsync(Action<IModel> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            return InvokeAsync(x =>
            {
                channelAction(x);
                return new NoContentStruct();
            });
        }

        public void Dispose()
        {
            cancellation.Cancel();
            persistentChannel.Dispose();
            channelSemaphore.Dispose();
        }

        private struct NoContentStruct
        {
        }
    }
}