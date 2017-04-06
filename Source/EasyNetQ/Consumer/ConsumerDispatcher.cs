using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EasyNetQ.Consumer
{
    public class ConsumerDispatcher : IConsumerDispatcher
    {
        private readonly BlockingCollection<Action> queue;
        private bool disposed;

        public ConsumerDispatcher(ConnectionConfiguration configuration, IEasyNetQLogger logger)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(logger, "logger");

            queue = new BlockingCollection<Action>();

            var thread = new Thread(_ =>
            {
                Action action;
                while (!disposed && queue.TryTake(out action, -1))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception exception)
                    {
                        logger.ErrorWrite(exception);
                    }
                }
            }) {Name = "EasyNetQ consumer dispatch thread", IsBackground = configuration.UseBackgroundThreads};
            thread.Start();
        }

        public void QueueAction(Action action)
        {
            Preconditions.CheckNotNull(action, "action");
            queue.Add(action);
        }

        public void OnDisconnected()
        {
            // throw away any queued actions. RabbitMQ will redeliver any in-flight
            // messages that have not been acked when the connection is lost.
            Action result;
            while (queue.TryTake(out result))
            {
            }
        }

        public void Dispose()
        {
            queue.CompleteAdding();
            disposed = true;
        }

        public bool IsDisposed => disposed;
    }
}