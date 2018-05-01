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
        private readonly AsyncBlockingQueue<Action<CancellationToken>> dispatchQueue;
        private readonly ManualResetEventSlim dispatchCompletedEvent = new ManualResetEventSlim(false);

        public ClientCommandDispatcherSingleton(
            ConnectionConfiguration configuration,
            IPersistentConnection connection,
            IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            persistentChannel = persistentChannelFactory.CreatePersistentChannel(connection);
            dispatchQueue = new AsyncBlockingQueue<Action<CancellationToken>>(QueueSize, dispatchCts.Token);
            StartDispatcherThread(configuration);
        }

        public async Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellation = default(CancellationToken))
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
                
            var tcs = CreateTsc<T>();
            using (cancellation.Register(() => TrySetCancelled(tcs, cancellation), false))
            {
                await dispatchQueue.EnqueueAsync(c =>
                {
                    using (var actionCts = CancellationTokenSource.CreateLinkedTokenSource(c, cancellation))
                    {
                        try
                        {
                            var result = persistentChannel.InvokeChannelAction(channelAction, actionCts.Token);
                            TrySetResult(tcs, result);
                        }
                        catch (OperationCanceledException exception)
                        {
                            TrySetCancelled(tcs, exception.CancellationToken);
                        }
                        catch (Exception exception)
                        {
                            TrySetException(tcs, exception);
                        }
                    }
                }, cancellation).ConfigureAwait(false);
                
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        public async Task InvokeAsync(Action<IModel> channelAction, CancellationToken cancellation = default(CancellationToken))
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            var tcs = CreateTsc<NoContentStruct>();
            using (cancellation.Register(() => TrySetCancelled(tcs, cancellation), false))
            {
                await dispatchQueue.EnqueueAsync(c =>
                {
                    using (var actionCts = CancellationTokenSource.CreateLinkedTokenSource(c, cancellation))
                    {
                        try
                        {
                            persistentChannel.InvokeChannelAction(channelAction, actionCts.Token);
                            TrySetResult(tcs, default(NoContentStruct));
                        }
                        catch (OperationCanceledException exception)
                        {
                            TrySetCancelled(tcs, exception.CancellationToken);
                        }
                        catch (Exception exception)
                        {
                            TrySetException(tcs, exception);
                        }
                    }
                }, cancellation).ConfigureAwait(false);
                
                await tcs.Task.ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            dispatchCts.Cancel();
            dispatchCompletedEvent.Wait();
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
                        var channelAction = dispatchQueue.Dequeue();
                        channelAction(dispatchCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                dispatchCompletedEvent.Set();
            }) {Name = "Client Command Dispatcher Thread", IsBackground = configuration.UseBackgroundThreads};
            thread.Start();
        }

        private struct NoContentStruct
        {
        }

        private static TaskCompletionSource<T> CreateTsc<T>()
        {
#if NETFX
            return new TaskCompletionSource<T>();
#else
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        }

        private static void TrySetCancelled<T>(TaskCompletionSource<T> tcs, CancellationToken cancellation)
        {
#if NETFX
            tcs.TrySetCanceledAsynchronously();   
#else
            tcs.TrySetCanceled(cancellation);
#endif
        }

        private static void TrySetException<T>(TaskCompletionSource<T> tcs, Exception e)
        {
#if NETFX
            tcs.TrySetExceptionAsynchronously(e);
#else
            tcs.TrySetException(e);
#endif
        }
        
        private static void TrySetResult<T>(TaskCompletionSource<T> tcs, T result)
        {
#if NETFX
            tcs.TrySetResultAsynchronously(result);
#else
            tcs.TrySetResult(result);
#endif
        }
    }
}