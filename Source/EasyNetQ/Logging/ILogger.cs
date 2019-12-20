using System;

namespace EasyNetQ.Logging
{
    public interface ILogger
    {
        bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters);
    }

    public interface ILogger<out TCategoryName> : ILogger
    {
    }
}
