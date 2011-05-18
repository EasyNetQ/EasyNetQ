using System.Reflection;
using System.Linq;
using System.Windows.Forms;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using EasyNetQ.Monitor.Services;

namespace EasyNetQ.Monitor.IoC
{
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Kernel.AddFacility<TypedFactoryFacility>();

            // register factories, can't currently do this with the fluent registration api.
            foreach (var factoryInterface in Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsInterface && x.Name.EndsWith("Factory")))
            {
                container.Register(Component.For(factoryInterface).AsFactory());
            }

            container.Register(
                Component.For<Main>(),

                AllTypes.FromThisAssembly().BasedOn<TreeNode>().Configure(x => x.LifeStyle.Transient),
                
                AllTypes.FromThisAssembly().Where(Component.IsInNamespace("EasyNetQ.Monitor.Controllers"))
                    .WithService.DefaultInterface()
                    .Configure(x => x.LifeStyle.Transient),

                Component.For<IRigService>().ImplementedBy<RigService>().LifeStyle.Transient
                );
        }
    }
}