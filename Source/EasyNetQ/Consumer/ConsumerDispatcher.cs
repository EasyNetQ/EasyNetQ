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
        private readonly BlockingCollection<Action> durableActions = new BlockingCollection<Action>();
        private readonly BlockingCollection<Action> transientActions = new BlockingCollection<Action>();

        public ConsumerDispatcher(ConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(configuration, "configuration");


            var thread = new Thread(_ =>
            {
                var blockingCollections = new[] {durableActions, transientActions};
                while (!cancellation.IsCancellationRequested)
                    try
                    {
                        BlockingCollection<Action>.TakeFromAny(
                            blockingCollections, out var action, cancellation.Token
                        );
                        action();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        logger.ErrorException(string.Empty, exception);
                    }

                while (BlockingCollection<Action>.TryTakeFromAny(blockingCollections, out var action) >= 0)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception exception)
                    {
                        logger.ErrorException(string.Empty, exception);
                    }
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

            if (cancellation.IsCancellationRequested)
                throw new InvalidOperationException("Consumer dispatcher is stopping or already stopped");

            if (surviveDisconnect)
                durableActions.Add(action);
            else
                transientActions.Add(action);
        }

        public void OnDisconnected()
        {
            // throw away any queued actions. RabbitMQ will redeliver any in-flight
            // messages that have not been acked when the connection is lost.
            while (transientActions.TryTake(out _))
            {
            }
        }

        public void Dispose()
        {
            durableActions.CompleteAdding();
            transientActions.CompleteAdding();
            cancellation.Cancel();
        }
    }
}
