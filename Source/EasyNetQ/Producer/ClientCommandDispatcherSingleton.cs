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
        private readonly BlockingCollection<Action<IModel>> queue = new BlockingCollection<Action<IModel>>(queueSize);
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly IPersistentConnection connection;
        private readonly IEasyNetQLogger logger;

        public ClientCommandDispatcherSingleton(IPersistentConnection connection, IEasyNetQLogger logger)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(logger, "logger");

            this.connection = connection;
            this.logger = logger;

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
                            Execute(channelAction);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }) {Name = "Client Command Dispatcher Thread"}.Start();
        }

        private void Execute(Action<IModel> channelAction)
        {
            try
            {
                // execute the channel action here.
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        public Task<T> Invoke<T>(Func<IModel, T> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            var tcs = new TaskCompletionSource<T>();

            try
            {
                queue.Add(x =>
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            tcs.SetCanceled();
                            return;
                        }
                        try
                        {
                            tcs.SetResult(channelAction(x));
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

        public void Dispose()
        {
            cancellation.Cancel();
        }
    }
}