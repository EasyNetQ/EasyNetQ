namespace EasyNetQ.Monitor.AlertSinks
{
    public class NullAlertSink : IAlertSink
    {
        public void Alert(string message)
        {
            // does nothing
        }
    }
}