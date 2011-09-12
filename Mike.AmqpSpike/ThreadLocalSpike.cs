using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mike.AmqpSpike
{
    public class ThreadLocalSpike
    {
        public void PlayWithThreadLocal()
        {
            var threadName = new ThreadLocal<string>(() => Thread.CurrentThread.ManagedThreadId.ToString());
            Action action = () =>
            {
                var repeat = threadName.IsValueCreated ? "(repeat)" : "";
                var name = threadName.Value;
                Console.Out.WriteLine("name = {0} {1}", name, repeat);
            };

            Parallel.Invoke(action, action, action, action, action, action, action, action);

            threadName.Dispose();
        } 
    }
}