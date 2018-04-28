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
        private const int QueueSize = 1;
        private readonly CancellationTokenSource dispatchCts = new CancellationTokenSource();
        private readonly IPersistentChannel persistentChannel;
        private readonly AsyncBlockingQueue<Action> queue = new AsyncBlockingQueue<Action>(QueueSize);

        public ClientCommandDispatcherSingleton(
            ConnectionConfiguration configuration,
            IPersistentConnection connection,
            IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            persistentChannel = persistentChannelFactory.CreatePersistentChannel(connection);

            StartDispatcherThread(configuration);
        }

        public async Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellation = default(CancellationToken))
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            using (var invocationCts = CancellationTokenSource.CreateLinkedTokenSource(dispatchCts.Token, cancellation))
            {
                var invocationCancellation = invocationCts.Token;
#if NETFX
                var tcs = new TaskCompletionSource<T>();
#else
                var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

                try
                {
                    await queue.EnqueueAsync(() =>
                    {
                        if (invocationCancellation.IsCancellationRequested)
                        {
#if NETFX
                            tcs.TrySetCanceledAsynchronously();   
#else
                            tcs.TrySetCanceled();
#endif

                            return;
                        }

                        try
                        {
                            persistentChannel.InvokeChannelAction(channel =>
                            {
                                if (invocationCancellation.IsCancellationRequested)
                                {
#if NETFX
                                    tcs.TrySetCanceledAsynchronously();   
#else
                                    tcs.TrySetCanceled();
#endif
                                }
                                else
                                {
#if NETFX
                                    tcs.TrySetResultAsynchronously(channelAction(channel));   
#else
                                    tcs.TrySetResult(channelAction(channel));
#endif
                                }
                            });
                        }
                        catch (Exception e)
                        {
#if NETFX
                            tcs.TrySetExceptionAsynchronously(e);   
#else
                            tcs.TrySetException(e);
#endif
                        }
                    }, invocationCancellation).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        public Task InvokeAsync(Action<IModel> channelAction, CancellationToken cancellation = default(CancellationToken))
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            return InvokeAsync(x =>
            {
                channelAction(x);
                return new NoContentStruct();
            }, cancellation);
        }

        public void Dispose()
        {
            dispatchCts.Cancel();
            persistentChannel.Dispose();
        }

        private void StartDispatcherThread(ConnectionConfiguration configuration)
        {
            var thread = new Thread(() =>
            {
                while (!dispatchCts.IsCancellationRequested)
                {
                    try
                    {
                        var channelAction = queue.Dequeue(dispatchCts.Token);
                        channelAction();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }) {Name = "Client Command Dispatcher Thread", IsBackground = configuration.UseBackgroundThreads};
            thread.Start();
        }

        private struct NoContentStruct
        {
        }
    }
}