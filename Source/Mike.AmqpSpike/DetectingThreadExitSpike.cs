using System;
using System.Threading;

namespace Mike.AmqpSpike
{
    public class DetectingThreadExitSpike
    {
        public void TheTest()
        {
            var client = new Client(new Library());

            client.DoWorkInAThread();

            // give the client thread time to complete
            Thread.Sleep(100);
        }
    }

    public class Library
    {
        public WorkScope GetWorkScope()
        {
            return new WorkScope();
        }

        public void StartSomething()
        {
            Console.WriteLine("Library says: StartSomething called");
        }
    }

    public class WorkScope : IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine("Library says: I can clean up");
        }
    }

    public class Client
    {
        private readonly Library library;

        public Client(Library library)
        {
            this.library = library;
        }

        public void DoWorkInAThread()
        {
            var thread = new Thread(() =>
            {
                using(library.GetWorkScope())
                {
                    library.StartSomething();
                    Console.WriteLine("Client thread says: I'm done");
                }
            });
            thread.Start();
        }
    }
}