using System;

namespace EasyNetQ.Loggers
{
    public class ConsoleLogger : IEasyNetQLogger
    {
        public void DebugWrite(string format, params object[] args)
        {
            SafeConsoleWrite("DEBUG: " + format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            SafeConsoleWrite("INFO: " + format, args);
        }

        public void ErrorWrite(string format, params object[] args)
        {
            SafeConsoleWrite("ERROR: " + format, args);
        }

        public void SafeConsoleWrite(string format, params object[] args)
        {
            // even a zero length args paramter causes WriteLine to interpret 'format' as
            // a format string. Rather than escape JSON, better to check the intention of 
            // the caller.
            if (args.Length == 0)
            {
                Console.WriteLine(format);
            }
            else
            {
                Console.WriteLine(format, args);
            }
        }

        public void ErrorWrite(Exception exception)
        {
            Console.WriteLine(exception.ToString());
        }
    }
}