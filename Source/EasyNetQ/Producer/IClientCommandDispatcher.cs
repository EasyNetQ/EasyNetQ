using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Responsible for invoking client commands.
    /// </summary>
    public interface IClientCommandDispatcher : IDisposable
    {
        Task<T> Invoke<T>(Func<IModel, T> channelAction);
    }

    /// <summary>
    /// Invokes client commands on a single channel. All commands are marshalled onto
    /// a single thread.
    /// </summary>
    public class ClientCommandDispatcher : IClientCommandDispatcher
    {
        private readonly Lazy<IClientCommandDispatcher> dispatcher;

        public ClientCommandDispatcher(IPersistentConnection connection, IEasyNetQLogger logger)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(logger, "logger");

            dispatcher = new Lazy<IClientCommandDispatcher>(() =>
                new ClientCommandDispatcherSingleton(connection, logger));
        }

        public Task<T> Invoke<T>(Func<IModel, T> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");
            return dispatcher.Value.Invoke(channelAction);
        }

        public void Dispose()
        {
            if(dispatcher.IsValueCreated) dispatcher.Value.Dispose();
        }
    }

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

    public interface IPersistentChannel : IDisposable
    {
        void InvokeChannelAction(Action<IModel> channelAction);
    }

    public class PersistentChannel : IPersistentChannel
    {
        private readonly IPersistentConnection connection;
        private readonly IEasyNetQLogger logger;

        private IModel channel;

        public PersistentChannel(IPersistentConnection connection, IEasyNetQLogger logger)
        {
            this.connection = connection;

            this.connection.Disconnected += () => channel = null;
            this.connection.Connected += () => channel = connection.CreateModel();

            this.logger = logger;
        }

        public IModel Channel
        {
            get
            {
                if (channel == null || !channel.IsOpen)
                {
                    channel = connection.CreateModel();
                }
                return channel;
            }
        }

        public void Dispose()
        {
            if(channel != null) channel.Dispose();
        }

        public void InvokeChannelAction(Action<IModel> channelAction)
        {
            throw new NotImplementedException();
        }
    }
}