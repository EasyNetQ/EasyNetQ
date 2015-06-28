using System;
using EasyNetQ.Loggers;

namespace EasyNetQ.Tests.Tasks
{
    public class NoDebugLogger : IEasyNetQLogger
    {
        private readonly ConsoleLogger logger = new ConsoleLogger();

        public void DebugWrite(string format, params object[] args)
        {

        }

        public void InfoWrite(string format, params object[] args)
        {

        }

        public void ErrorWrite(string format, params object[] args)
        {
            logger.ErrorWrite(format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            logger.ErrorWrite(exception);
        }
    }
}