using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public class ClientCommandDispatcherSingleton : IClientCommandDispatcher
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly IPersistentChannel persistentChannel;
        private readonly BlockingCollection<Action> queue;

        public ClientCommandDispatcherSingleton(
            ConnectionConfiguration configuration,
            IPersistentConnection connection,
            IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            queue = new BlockingCollection<Action>(configuration.DispatcherQueueSize);
            persistentChannel = persistentChannelFactory.CreatePersistentChannel(connection);

            StartDispatcherThread(configuration);
        }

        public Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            var tcs = TaskHelpers.CreateTcs<T>();

            try
            {
                queue.Add(() =>
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        tcs.TrySetCanceledAsynchronously();
                        return;
                    }

                    try
                    {
                        persistentChannel.InvokeChannelAction(channel => tcs.TrySetResultAsynchronously(channelAction(channel)));
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetExceptionAsynchronously(e);
                    }
                }, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceledAsynchronously();
            }

            return tcs.Task;
        }

        public Task InvokeAsync(Action<IModel> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            return InvokeAsync(x =>
            {
                channelAction(x);
                return new NoContentStruct();
            }, cancellationToken);
        }

        public void Dispose()
        {
            cancellation.Cancel();
            persistentChannel.Dispose();
        }

        private void StartDispatcherThread(ConnectionConfiguration configuration)
        {
            var thread = new Thread(() =>
            {
                while (!cancellation.IsCancellationRequested)
                    try
                    {
                        var channelAction = queue.Take(cancellation.Token);
                        channelAction();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
            }) { Name = "Client Command Dispatcher Thread", IsBackground = configuration.UseBackgroundThreads };
            thread.Start();
        }

        private struct NoContentStruct
        {
        }
    }
}
