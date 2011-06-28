using System;

namespace EasyNetQ.Loggers
{
    public class ConsoleLogger : IEasyNetQLogger
    {
        public void DebugWrite(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}