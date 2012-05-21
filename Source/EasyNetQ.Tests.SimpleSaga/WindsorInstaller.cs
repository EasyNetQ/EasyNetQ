using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace EasyNetQ.Tests.SimpleSaga
{
    public class WindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            Console.WriteLine("Running installer for SimpleSaga");
            container.Register(
                Component.For<ISaga>().ImplementedBy<TestSaga>().LifeStyle.Singleton
                );
        }
    }
}