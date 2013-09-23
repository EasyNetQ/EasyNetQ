using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EasyNetQ.Consumer
{
    public class ConsumerDispatcher : IConsumerDispatcher
    {
        private readonly Thread dispatchThread;
        private readonly BlockingCollection<Action> queue = new BlockingCollection<Action>();
        private bool disposed;

        public ConsumerDispatcher(IEasyNetQLogger logger)
        {
            Preconditions.CheckNotNull(logger, "logger");

            dispatchThread = new Thread(_ =>
                {
                    try
                    {
                        while (true)
                        {
                            if (disposed) break;

                            queue.Take()();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // InvalidOperationException is thrown when Take is called after 
                        // queue.CompleteAdding(), this is signals that this class is being
                        // disposed, so we allow the thread to complete.
                    }
                    catch (Exception exception)
                    {
                        logger.ErrorWrite(exception);
                    }
                }) { Name = "EasyNetQ consumer dispatch thread" };
            dispatchThread.Start();
        }

        public void QueueAction(Action action)
        {
            Preconditions.CheckNotNull(action, "action");
            queue.Add(action);
        }

        public void Dispose()
        {
            queue.CompleteAdding();
            disposed = true;
        }

        public bool IsDisposed
        {
            get { return disposed; }
        }
    }
}