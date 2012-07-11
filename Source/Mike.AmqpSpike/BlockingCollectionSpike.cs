using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mike.AmqpSpike
{
    public class BlockingCollectionSpike
    {
        public void Spike()
        {
            var queue = new BlockingCollection<int>
            {
                1, 
                2, 
                3, 
                4
            };

            var cancellationToken = new CancellationToken();

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                // queue.Dispose();
                queue.CompleteAdding();
                Console.WriteLine("Line after dispose");
            });

            Console.WriteLine("Waiting on Take");
            try
            {
                while (true)
                {
                    var item = queue.Take(cancellationToken);
                    Console.Out.WriteLine("Taken Item {0}", item);
                    Thread.Sleep(100);
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                Console.WriteLine(invalidOperationException);
            }
            Console.WriteLine("Line after take");
        }
     
        public void Spike2()
        {
            var queue = new BlockingCollection<int>();

            queue.CompleteAdding();
            queue.Add(1);
        }
    }
}