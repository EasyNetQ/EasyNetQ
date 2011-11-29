using System;
using EasyNetQ.Loggers;

namespace EasyNetQ.Tests
{
    public class MockLogger : IEasyNetQLogger
    {
        private readonly ConsoleLogger consoleLogger = new ConsoleLogger();

        public Action<string, object[]> DebugWriteCallback { get; set; }
        public Action<string, object[]> InfoWriteCallback { get; set; }
        public Action<string, object[]> ErrorWriteCallback { get; set; }
        public Action<Exception> ErrorWriteWithErrorCallback { get; set; }

        public void DebugWrite(string format, params object[] args)
        {
            consoleLogger.DebugWrite(format, args);
            if (DebugWriteCallback != null) DebugWriteCallback(format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            consoleLogger.InfoWrite(format, args);
            if (InfoWriteCallback != null) InfoWriteCallback(format, args);
        }

        public void ErrorWrite(string format, params object[] args)
        {
            consoleLogger.ErrorWrite(format, args);
            if (ErrorWriteCallback != null) ErrorWriteCallback(format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            consoleLogger.ErrorWrite(exception);
            if (ErrorWriteWithErrorCallback != null) ErrorWriteWithErrorCallback(exception);
        }
    }
}