using System;

namespace EasyNetQ.Loggers
{
    /// <summary>
    /// noop logger
    /// </summary>
    public class NullLogger : IEasyNetQLogger
    {
        public void DebugWrite(string format, params object[] args)
        {
            
        }

        public void InfoWrite(string format, params object[] args)
        {
            
        }

        public void ErrorWrite(string format, params object[] args)
        {
            
        }

        public void ErrorWrite(Exception exception)
        {
            
        }
    }
}