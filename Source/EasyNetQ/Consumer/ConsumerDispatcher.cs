using System;
using System.Collections.Concurrent;
using System.Threading;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer
{
    public class ConsumerDispatcher : IConsumerDispatcher
    {
        private readonly ILog logger = LogProvider.For<ConsumerDispatcher>();
        private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        private readonly ConcurrentQueue<Action> durableActions = new ConcurrentQueue<Action>();
        private readonly ConcurrentQueue<Action> transientActions = new ConcurrentQueue<Action>();

        public ConsumerDispatcher(ConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            var thread = new Thread(_ =>
            {
                while(!IsDone())
                    try
                    {
                        if (durableActions.TryDequeue(out var action) || transientActions.TryDequeue(out action))
                            action();
                        else
                            autoResetEvent.WaitOne();
                    }
                    catch (Exception exception)
                    {
                        logger.ErrorException(string.Empty, exception);
                    }
            }) {Name = "EasyNetQ consumer dispatch thread", IsBackground = configuration.UseBackgroundThreads};
            thread.Start();
        }

        public bool IsDisposed { get; private set; }
        public void QueueAction(Action action)
        {
            QueueAction(action, false);
        }

        public void QueueAction(Action action, bool surviveDisconnect)
        {
            Preconditions.CheckNotNull(action, "action");
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ConsumerDispatcher));

            if (surviveDisconnect)
            {
                durableActions.Enqueue(action);
            }
            else
            {
                transientActions.Enqueue(action);
            }
            autoResetEvent.Set();
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
            IsDisposed = true;
            autoResetEvent.Set();
        }

        private bool IsDone()
        {
            return IsDisposed && durableActions.IsEmpty && transientActions.IsEmpty;
        }
    }
}
