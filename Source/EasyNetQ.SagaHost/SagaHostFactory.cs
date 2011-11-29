namespace EasyNetQ.SagaHost
{
    /// <summary>
    /// Poor man's dependency injection. Provides a factory to create a default instance of
    /// ISagaHost
    /// </summary>
    public class SagaHostFactory
    {
        public static ISagaHost CreateSagaHost(string sagaDirectory)
        {
            var bus = RabbitHutch.CreateBus();
            return new DefaultSagaHost(bus, log4net.LogManager.GetLogger(typeof(DefaultSagaHost)), sagaDirectory);
        }
    }
}