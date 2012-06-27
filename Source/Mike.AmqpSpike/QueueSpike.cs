using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mike.AmqpSpike
{
    public class QueueSpike
    {
        public void HowDoesAConcurrentQueueBehave()
        {
            const int numberOfItemsToQueue = 100;

            var autoResetEvent = new CountdownEvent(numberOfItemsToQueue);

            var queue = new ConcurrentQueue<QueuedItem>();

            Action<int> consumerStarter = n => Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    QueuedItem item;
                    if(queue.TryDequeue(out item))
                    {
                        Console.WriteLine("{0}:{1}", n, item.Text);
                        autoResetEvent.Signal();
                        Thread.Sleep(1);
                    }
                    else
                    {
                        Console.WriteLine("Nothing on queue");
                        Thread.Sleep(100);
                    }
                }
            });

            consumerStarter(1);
            consumerStarter(2);
            consumerStarter(3);
            
            Thread.Sleep(100);

            for (var i = 0; i < numberOfItemsToQueue; i++)
            {
                queue.Enqueue(new QueuedItem(string.Format("{0}", i)));
            }

            autoResetEvent.Wait(1000);
        }     
    }

    public class QueuedItem
    {
        public QueuedItem(string text)
        {
            Text = text;
        }

        public string Text { get; private set; }
    }
}