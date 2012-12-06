// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.MicroKernel;
using Castle.MicroKernel.Handlers;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Diagnostics;
using NUnit.Framework;

namespace EasyNetQ.Monitor.Tests
{
    [TestFixture]
    public class InstallerTests
    {
        private IWindsorInstaller installer;
        private IWindsorContainer container;

        [SetUp]
        public void SetUp()
        {
            installer = new Installer();
            container = new WindsorContainer();
            installer.Install(container, null);

        }

        [Test]
        public void Should_create_components()
        {
            CheckForPotentiallyMisconfiguredComponents(container);
        }

        [Test]
        public void Should_resolve_brokers()
        {
            var brokers = container.Resolve<IEnumerable<Broker>>();

            brokers.Count().ShouldEqual(1);
            brokers.First().ManagementUrl.ShouldEqual("http://localhost");
        }

        [Test]
        public void Should_resolve_checks()
        {
            var checks = container.ResolveAll<ICheck>();

            checks.Count().ShouldEqual(5);
        }

        private static void CheckForPotentiallyMisconfiguredComponents(IWindsorContainer container)
        {
            var host = (IDiagnosticsHost)container.Kernel.GetSubSystem(SubSystemConstants.DiagnosticsKey);
            var diagnostics = host.GetDiagnostic<IPotentiallyMisconfiguredComponentsDiagnostic>();

            var handlers = diagnostics.Inspect();

            if (handlers.Any())
            {
                var message = new StringBuilder();
                var inspector = new DependencyInspector(message);

                foreach (IExposeDependencyInfo handler in handlers)
                {
                    handler.ObtainDependencyDetails(inspector);
                }

                throw new ApplicationException(message.ToString());
            }
        }
    }


}

// ReSharper restore InconsistentNaming