namespace EasyNetQ.SagaHost
{
    public interface ISagaHost
    {
        void Start();
        void Stop();
    }
}