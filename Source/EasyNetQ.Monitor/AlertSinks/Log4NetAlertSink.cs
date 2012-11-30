using log4net;

namespace EasyNetQ.Monitor.AlertSinks
{
    public class Log4NetAlertSink : IAlertSink
    {
        private readonly ILog log;

        public Log4NetAlertSink(ILog log)
        {
            this.log = log;
        }

        public void Alert(string message)
        {
            log.Info(string.Format("Alert message: '{0}'", message));
        }
    }
}