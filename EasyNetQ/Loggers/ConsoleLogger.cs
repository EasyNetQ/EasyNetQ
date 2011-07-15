using System;

namespace EasyNetQ.Loggers
{
    public class ConsoleLogger : IEasyNetQLogger
    {
        public void DebugWrite(string format, params object[] args)
        {
            Console.WriteLine("DEBUG: " + format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            Console.WriteLine("INFO: " + format, args);
        }

        public void ErrorWrite(string format, params object[] args)
        {
            Console.WriteLine("ERROR: " + format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            Console.WriteLine("ERROR");
            Console.WriteLine(exception.ToString());
        }
    }
}