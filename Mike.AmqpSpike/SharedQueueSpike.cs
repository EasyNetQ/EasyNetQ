using System;
using System.Threading;
using RabbitMQ.Util;

namespace Mike.AmqpSpike
{
    public class SharedQueueSpike
    {
        public void CloseBehviour()
        {
            var queue = new SharedQueue();

            queue.Enqueue("Hello World");
            //queue.Close();

            var item = queue.Dequeue().ToString();
            Console.Out.WriteLine("item = {0}", item);

            ThreadPool.QueueUserWorkItem(state =>
            {
                // expect the queue to block here
                Console.WriteLine("Dequeue in thread");
                try
                {
                    var item2 = queue.Dequeue().ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in thread\n{0}", e.ToString());
                }
            });

            Thread.Sleep(100);
            Console.WriteLine("Closing queue");
            queue.Close();

            Console.WriteLine("Done");
        }
    }
}