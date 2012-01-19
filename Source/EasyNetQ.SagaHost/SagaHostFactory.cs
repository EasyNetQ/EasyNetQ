using System;
using System.Configuration;

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
            var log = log4net.LogManager.GetLogger(typeof (DefaultSagaHost));

            var containerName = ConfigurationManager.AppSettings.Get("container");
            switch (containerName)
            {
                case "windsor" :
                    Console.WriteLine("Using windsor ########################################");
                    return new WindsorSagaHost(bus, log, sagaDirectory);
                default :
                    return new DefaultSagaHost(bus, log, sagaDirectory);
            }
        }
    }
}