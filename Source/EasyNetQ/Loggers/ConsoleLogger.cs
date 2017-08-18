using System;

namespace EasyNetQ.Loggers
{
    public class ConsoleLogger : IEasyNetQLogger
    {
        public bool Debug { get; set; }
        public bool Info { get; set; }
        public bool Error { get; set; }

        public ConsoleLogger()
        {
            Debug = true;
            Info = true;
            Error = true;
        }

        public void DebugWrite(string format, params object[] args)
        {
            if (!Debug) return;
            Console.WriteLine("DEBUG: " + format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            if (!Info) return;
            Console.WriteLine("INFO: " + format, args);
        }

        public void ErrorWrite(string format, params object[] args)
        {
            if (!Error) return;
            Console.WriteLine("ERROR: " + format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            Console.WriteLine(exception.ToString());
        }
    }
}