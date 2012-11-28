namespace EasyNetQ.Monitor
{
    public interface IAlertSink
    {
        void Alert(string message);
    }
}