using System;
using System.IO;
using log4net.Config;
using Topshelf;

namespace EasyNetQ.SagaHost
{
    class Program
    {
        static void Main()
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo(".\\log4net.config"));

            HostFactory.Run(hostConfiguration =>
            {
                hostConfiguration.AfterStartingServices(() => Console.WriteLine("Started EasyNetQ.SagaHost"));
                hostConfiguration.AfterStoppingServices(() => Console.WriteLine("Stopped EasyNetQ.SagaHost"));
                // hostConfiguration.EnableDashboard();
                hostConfiguration.RunAsLocalSystem();
                hostConfiguration.SetDescription("EasyNetQ.SagaHost");
                hostConfiguration.SetDisplayName("EasyNetQ.SagaHost");
                hostConfiguration.SetServiceName("EasyNetQ.SagaHost");

                hostConfiguration.Service<ISagaHost>(serviceConfiguration =>
                {
                    serviceConfiguration.SetServiceName("SagaHostingService");
                    serviceConfiguration.ConstructUsing(name => SagaHostFactory.CreateSagaHost());

                    serviceConfiguration.WhenStarted(sagaHostingService => sagaHostingService.Start());
                    serviceConfiguration.WhenStopped(sagaHostingService => sagaHostingService.Stop());
                });
            });
        }
    }
}
