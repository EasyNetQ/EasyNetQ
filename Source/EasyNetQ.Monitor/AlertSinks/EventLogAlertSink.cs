using System.Diagnostics;

namespace EasyNetQ.Monitor.AlertSinks
{
    public class EventLogAlertSink : IAlertSink
    {
        private const string sourceName = "EasyNetQ.Monitor";
        private const int eventId = 2112;

        public EventLogAlertSink()
        {
            if (!EventLog.SourceExists(sourceName))
            {
                EventLog.CreateEventSource(sourceName, "Application");
            }
        }

        public void Alert(string message)
        {
            EventLog.WriteEntry(sourceName, message, EventLogEntryType.Warning, eventId);
        }
    }
}