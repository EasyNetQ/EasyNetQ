using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer
{
    public class ConsumerDispatcher : IConsumerDispatcher
    {
        private readonly ILog logger = LogProvider.For<ConsumerDispatcher>();
        private readonly ConcurrentQueue<Action> highPriority = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<Action> mediumPriority = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<Action> lowPriority = new ConcurrentQueue<Action>();
        private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        private bool disposed;

        public ConsumerDispatcher(ConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            var thread = new Thread(_ =>
            {
                while(!IsDone())
                {
                    try
                    {
                        if (highPriority.TryDequeue(out var action) || mediumPriority.TryDequeue(out action) || lowPriority.TryDequeue(out action))
                        {
                            action();
                        }
                        else
                        {
                            manualResetEvent.WaitOne();
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.ErrorException(string.Empty, exception);
                    }
                }
            }) { Name = "EasyNetQ consumer dispatch thread", IsBackground = configuration.UseBackgroundThreads };
            thread.Start();
        }

        public bool IsDone()
        {
            return disposed && highPriority.IsEmpty && mediumPriority.IsEmpty && lowPriority.IsEmpty;
        }

        public void QueueAction(Action action, Priority priority = Priority.Low)
        {
            Preconditions.CheckNotNull(action, "action");

            switch (priority)
            {
                case Priority.Low:
                    lowPriority.Enqueue(action);
                    break;
                case Priority.Medium:
                    mediumPriority.Enqueue(action);
                    break;
                case Priority.High:
                    highPriority.Enqueue(action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(priority), priority, null);
            }

            manualResetEvent.Set();
        }

        public void OnDisconnected()
        {
            QueueAction(() =>
            {
                // throw away any queued actions. RabbitMQ will redeliver any in-flight
                // messages that have not been acked when the connection is lost.
                while (lowPriority.TryDequeue(out _))
                {
                }
            }, Priority.High);
        }

        public void Dispose()
        {
            disposed = true;
            manualResetEvent.Set();
        }

        public bool IsDisposed => disposed;
    }
}
