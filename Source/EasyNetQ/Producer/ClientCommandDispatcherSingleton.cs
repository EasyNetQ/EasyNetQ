using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public class ClientCommandDispatcherSingleton : IClientCommandDispatcher
    {
        private const int queueSize = 1;
        private readonly BlockingCollection<Action> queue = new BlockingCollection<Action>(queueSize);
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly IPersistentChannel persistentChannel;

        public ClientCommandDispatcherSingleton(
            IPersistentConnection connection,
            IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            persistentChannel = persistentChannelFactory.CreatePersistentChannel(connection);

            StartDispatcherThread();
        }

        private void StartDispatcherThread()
        {
            new Thread(() =>
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
                }) {Name = "Client Command Dispatcher Thread"}.Start();
        }

        public Task<T> InvokeAsync<T>(Func<IModel, T> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            var tcs = new TaskCompletionSource<T>();

            try
            {
                queue.Add(() =>
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            tcs.SetCanceled();
                            return;
                        }
                        try
                        {
                            persistentChannel.InvokeChannelAction(channel => tcs.SetResult(channelAction(channel)));
                        }
                        catch (Exception e)
                        {
                            tcs.SetException(e);
                        }
                    }, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                tcs.SetCanceled();
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
            cancellation.Cancel();
            persistentChannel.Dispose();
        }

        private struct NoContentStruct {}
    }
}