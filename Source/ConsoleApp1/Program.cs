using EasyNetQ;
using EasyNetQ.Logging;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection()
                .RegisterEasyNetQ("host=localhost", r => r.Register(typeof(ILogger<>), typeof(ConsoleTestLogger<>)).Register(typeof(ILogger), typeof(ConsoleTestLogger)));
            var provider = services.BuildServiceProvider();

            var bus = provider.GetService<IAdvancedBus>();
            bus.QueueDeclare("aaa");
            bus.Publish(Exchange.GetDefault(), "aaa", false, new Message<string>("bbb"));

            Console.ReadLine();
        }
    }

    public class ConsoleTestLogger : ILogger
    {
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            if (messageFunc == null)
                return true;
            Console.WriteLine(messageFunc());
            return true;
        }
    }

    public class ConsoleTestLogger<TCategory> : ILogger<TCategory>
    {
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            if (messageFunc == null)
                return true;
            Console.WriteLine(messageFunc());
            return true;
        }
    }
}
