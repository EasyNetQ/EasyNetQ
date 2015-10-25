using System;
using Serilog;

namespace EasyNetQ.Serilog
{
    public class SerilogLogger : IEasyNetQLogger
    {
        private readonly ILogger logger;

        public SerilogLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public void DebugWrite(string format, params object[] args)
        {
            logger.Debug(format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            logger.Information(format, args);
        }

        public void ErrorWrite(string format, params object[] args)
        {
            logger.Error(format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            logger.Error(exception, "Exception occured: {exception}", exception);
        }
    }
}
