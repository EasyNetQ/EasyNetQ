using System;
using System.Threading;
using EasyNetQ.Loggers;

namespace EasyNetQ.TestingRecovery
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus("host=localhost:5673,localhost:5674", s => s.Register<IEasyNetQLogger>(p => new ConsoleLogger()));

            SpinWait.SpinUntil(() => bus.IsConnected);

            bus.Respond<MyRequest, MyResponse>(request => new MyResponse(), config => config.WithDurable(true));

            while (true)
            {
                try
                {
                    var response = bus.Request<MyRequest, MyResponse>(new MyRequest());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                Thread.Sleep(1000);
            }

            bus.Dispose();
        }
    }

    public class MyMessage
    {
        public Guid Id { get; set; }
        public string Data { get; set; }
    }

    public class MyRequest
    {
    }

    public class MyResponse
    {
    }
}