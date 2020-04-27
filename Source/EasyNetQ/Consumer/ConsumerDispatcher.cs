using System;
using System.Collections.Concurrent;
using System.Threading;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer
{
    public class ConsumerDispatcher : IConsumerDispatcher
    {
        private readonly ILog logger = LogProvider.For<ConsumerDispatcher>();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly SemaphoreSlim actionsAvailable = new SemaphoreSlim(0);
        private readonly ConcurrentQueue<Action> durableActions = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<Action> transientActions = new ConcurrentQueue<Action>();

        public ConsumerDispatcher(ConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            var thread = new Thread(_ =>
            {
                while (!cancellation.IsCancellationRequested || !durableActions.IsEmpty || !transientActions.IsEmpty)
                    try
                    {
                        if (durableActions.TryDequeue(out var action) || transientActions.TryDequeue(out action))
                            action();
                        else
                            actionsAvailable.Wait(cancellation.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        logger.ErrorException(string.Empty, exception);
                    }
            }) {Name = "EasyNetQ consumer dispatch thread", IsBackground = configuration.UseBackgroundThreads};
            thread.Start();
        }

        public void QueueAction(Action action)
        {
            QueueAction(action, false);
        }

        public void QueueAction(Action action, bool surviveDisconnect)
        {
            Preconditions.CheckNotNull(action, "action");

            if(cancellation.IsCancellationRequested)
                throw new InvalidOperationException("Consumer dispatcher is stopping or already stopped");

            if (surviveDisconnect)
                durableActions.Enqueue(action);
            else
                transientActions.Enqueue(action);

            actionsAvailable.Release();
        }

        public void OnDisconnected()
        {
            // throw away any queued actions. RabbitMQ will redeliver any in-flight
            // messages that have not been acked when the connection is lost.
            while (transientActions.TryDequeue(out _))
            {
            }
        }

        public void Dispose()
        {
            cancellation.Cancel();
        }
    }
}
