using EasyNetQ.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Producer
{
    public class ClientCommandDispatcherSingleton : IClientCommandDispatcher
    {
        private readonly ILog logger = LogProvider.For<ClientCommandDispatcherSingleton>();
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

            using (ExecutionContext.SuppressFlow())
                StartDispatcherThread(configuration);
        }

        public T Invoke<T>(Func<IModel, T> channelAction)
        {
            return InvokeAsync(channelAction).GetAwaiter().GetResult();
        }

        public void Invoke(Action<IModel> channelAction)
        {
            InvokeAsync(channelAction).GetAwaiter().GetResult();
        }

        public Task<T> InvokeAsync<T>(Func<IModel, T> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                queue.Add(() =>
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }
                    try
                    {
                        persistentChannel.InvokeChannelAction(channel =>
                        {
                            tcs.TrySetResult(channelAction(channel));
                        });
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                    }
                }, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            return tcs.Task;
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
            queue.CompleteAdding();
            cancellation.Cancel();
            persistentChannel.Dispose();
        }

        private void StartDispatcherThread(ConnectionConfiguration configuration)
        {
            var thread = new Thread(() =>
            {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        if (queue.TryTake(out var channelAction, Timeout.Infinite, cancellation.Token))
                        {
                            channelAction();
                        }
                    }
                    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                    {
                        break;
                    }
                }
                logger.Debug("EasyNetQ client command dispatch thread finished");
            })
            { Name = "EasyNetQ client command dispatch thread", IsBackground = configuration.UseBackgroundThreads };
            thread.Start();
            logger.Debug("EasyNetQ client command dispatch thread started");
        }

        private struct NoContentStruct
        {
        }
    }
}
