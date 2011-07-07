using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mike.AmqpSpike
{
    public class TaskCompletionSourceSpike
    {
        public void UseRunAsync()
        {
            var task = RunAsync(() => "Hello");

            task.ContinueWith(t => Console.WriteLine(t.Result));

            Thread.Sleep(100);
        }

        public static Task<T> RunAsync<T>(Func<T> func)
        {
            if(func == null)
            {
                throw new ArgumentNullException("func");
            }

            var tcs = new TaskCompletionSource<T>();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }
            });

            return tcs.Task;
        }

        public void UseRunDelayed()
        {
            var task = RunDelayed(500, () => "Hello");

            task.ContinueWith(t => Console.WriteLine(t.Result));

            Thread.Sleep(1000);
        }

        public static Task<T> RunDelayed<T>(int millisecondsDelay, Func<T> func)
        {
            if(func == null)
            {
                throw new ArgumentNullException("func");
            }
            if (millisecondsDelay < 0)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay");
            }

            var taskCompletionSource = new TaskCompletionSource<T>();

            var timer = new Timer(self =>
            {
                ((Timer) self).Dispose();
                try
                {
                    var result = func();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception exception)
                {
                    taskCompletionSource.SetException(exception);
                }
            });
            timer.Change(millisecondsDelay, millisecondsDelay);

            return taskCompletionSource.Task;
        }


        public void Foo()
        {
            var batch = new[] {1, 2, 3, 4, 5};

            foreach (var item in batch)
            {
                var itemToProcess = item;
                var thread = new Thread(_ => ProcessItem(itemToProcess));
                thread.Start();
            }
            Thread.Sleep(10);
        }

        public void ProcessItem(int item)
        {
            Console.WriteLine(item);
        }
    }

}