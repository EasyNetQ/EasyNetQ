using System;

namespace EasyNetQ.Loggers
{
    public class DelegateLogger : IEasyNetQLogger
    {
        public Action<string, object[]> DefaultWrite { get; set; }
        public Action<string, object[]> DebugWriteDelegate { get; set; }
        public Action<string, object[]> InfoWriteDelegate { get; set; }
        public Action<string, object[]> ErrorWriteDelegate { get; set; }

        public DelegateLogger()
        {
            DefaultWrite = (s, o) => { };
        }

        public void DebugWrite(string format, params object[] args)
        {
            if (DebugWriteDelegate == null)
            {
                DefaultWrite(format, args);
            }
            else
            {
                DebugWriteDelegate(format, args);
            }
        }

        public void InfoWrite(string format, params object[] args)
        {
            if (InfoWriteDelegate == null)
            {
                DefaultWrite(format, args);
            }
            else
            {
                InfoWriteDelegate(format, args);
            }
        }

        public void ErrorWrite(string format, params object[] args)
        {
            if (ErrorWriteDelegate == null)
            {
                DefaultWrite(format, args);
            }
            else
            {
                ErrorWriteDelegate(format, args);
            }
        }

        public void ErrorWrite(Exception exception)
        {
            if (ErrorWriteDelegate == null)
            {
                DefaultWrite(exception.ToString(), new object[0]);
            }
            else
            {
                ErrorWriteDelegate(exception.ToString(), new object[0]);
            }
        }
    }
}