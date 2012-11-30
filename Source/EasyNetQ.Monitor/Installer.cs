using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using log4net;
using log4net.Config;

namespace EasyNetQ.Monitor
{
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            if(container == null)
            {
                throw new ArgumentNullException("container");
            }

            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel, false));
            container.Kernel.Resolver.AddSubResolver(new AppSettingsConvention());

            XmlConfigurator.Configure();
            var logger = LogManager.GetLogger("EasyNetQ.Monitor");
            var settings = MonitorConfigurationSection.Settings;

            container.Register(
                Component.For<ILog>().Instance(logger),
                Component.For<IMonitorService>().ImplementedBy<MonitorService>(),
                Component.For<IMonitorRun>().ImplementedBy<MonitorRun>(),
                Component.For<IManagementClientFactory>().ImplementedBy<ManagementClientFactory>(),
                Component.For<MonitorConfigurationSection>().Instance(settings),
                Component.For<IEnumerable<Broker>>().UsingFactoryMethod(k => GetBrokers(settings)),
                Classes.FromThisAssembly().BasedOn<ICheck>().WithServiceFirstInterface(),
                Classes.FromThisAssembly().BasedOn<IAlertSink>().WithServiceFirstInterface()
                );
        }

        private static IEnumerable<Broker> GetBrokers(MonitorConfigurationSection configuration)
        {
            return configuration.Brokers.Cast<object>().Cast<Broker>();
        }
    }
}