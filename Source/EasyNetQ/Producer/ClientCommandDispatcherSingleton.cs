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
        private readonly AsyncBlockingQueue<Action> dispatchQueue = new AsyncBlockingQueue<Action>(QueueSize);
        private readonly ManualResetEventSlim dispatchEndEvent = new ManualResetEventSlim(false);

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
                var tcs = CreateTsc<T>();
                var invocationCancellation = invocationCts.Token;
                using (invocationCancellation.Register(() => TrySetCancelled(tcs, invocationCancellation), false))
                {
                    await dispatchQueue.EnqueueAsync(() =>
                    {
                        try
                        {
                            var result = persistentChannel.InvokeChannelAction(channelAction, invocationCancellation);
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
                    }, invocationCancellation).ConfigureAwait(false);
                    
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
        }

        public async Task InvokeAsync(Action<IModel> channelAction, CancellationToken cancellation = default(CancellationToken))
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            Preconditions.CheckNotNull(channelAction, "channelAction");
                
            using (var invocationCts = CancellationTokenSource.CreateLinkedTokenSource(dispatchCts.Token, cancellation))
            {
                var tcs = CreateTsc<NoContentStruct>();
                var invocationCancellation = invocationCts.Token;
                using (invocationCancellation.Register(() => TrySetCancelled(tcs, invocationCancellation), false))
                {
                    await dispatchQueue.EnqueueAsync(() =>
                    {
                        try
                        {
                            persistentChannel.InvokeChannelAction(channelAction, invocationCancellation);
                            TrySetResult(tcs, new NoContentStruct());
                        }
                        catch (OperationCanceledException exception)
                        {
                            TrySetCancelled(tcs, exception.CancellationToken);
                        }
                        catch (Exception exception)
                        {
                            TrySetException(tcs, exception);
                        }
                    }, invocationCancellation).ConfigureAwait(false);
                    
                    await tcs.Task.ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            dispatchCts.Cancel();
            dispatchEndEvent.Wait();
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
                        var channelAction = dispatchQueue.Dequeue(dispatchCts.Token);
                        channelAction();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                while (dispatchQueue.TryDequeue(out _))
                {
                }

                dispatchEndEvent.Set();
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
            tcs.TrySetResult(result);
#else
            tcs.TrySetResult(result);
#endif
        }
    }
}