﻿using System;
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
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly IPersistentChannel persistentChannel;
        private readonly BlockingCollection<Action> queue = new BlockingCollection<Action>(QueueSize);

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

        public Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

#if NETFX
            var tcs = new TaskCompletionSource<T>();
#else
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

            try
            {
                queue.Add(() =>
                {
                    if (cancellation.IsCancellationRequested)
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
#if NETFX                               
                            tcs.TrySetResultAsynchronously(channelAction(channel));   
#else
                            tcs.TrySetResult(channelAction(channel));
#endif                      
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
                }, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
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
                {
                    try
                    {
                        var channelAction = queue.Take(cancellation.Token);
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