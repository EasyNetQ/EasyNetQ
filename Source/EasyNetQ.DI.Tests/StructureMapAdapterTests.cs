using System;
using EasyNetQ.Tests;
using EasyNetQ.Tests.Mocking;
using StructureMap;
using Xunit;

namespace EasyNetQ.DI.Tests
{
    /// <summary>
    /// EasyNetQ expects that the DI container will follow a first-to-register-wins
    /// policy. The internal DefaultServiceProvider works this way, as does Windsor.
    /// However, Ninject doesn't allow more than one registration of a service, it 
    /// throws an exception. StructureMap has a last-to-register-wins policy 
    /// by default which has been overrided in the Adapter implementation.
    /// </summary>
    [Explicit("Starts a connection to localhost")]
    public class StructureMapAdapterTests : IDisposable
    {
        private StructureMap.IContainer container;
        private IContainer easynetQContainer;
        private IBus bus;

        public StructureMapAdapterTests()
        {
            container = new Container();

            container.RegisterAsEasyNetQContainerFactory();

            bus = new MockBuilder().Bus;

            easynetQContainer = bus.Advanced.Container;
        }

        [Fact]
        public void Should_create_bus_with_structure_map_adapter()
        {
            Assert.NotNull(bus);
        }

        [Fact]
        public void Should_utilize_first_in_wins_registration_strategy_for_an_interface()
        {
            easynetQContainer.Register<IComponent>(sp => new FirstComponent());
            easynetQContainer.Register<IComponent>(sp => new SecondComponent());
            
            Assert.IsAssignableFrom<FirstComponent>(easynetQContainer.Resolve<IComponent>());
        }

        [Fact]
        public void Should_utilize_first_in_wins_registration_strategy_for_a_named_method()
        {
            easynetQContainer.Register<Func<int>>(sp => One);
            easynetQContainer.Register<Func<int>>(sp => Two);

            Assert.Equal(1, easynetQContainer.Resolve<Func<int>>()());
        }

        [Fact]
        public void Should_utilize_first_in_wins_registration_strategy_for_a_lambda_expression()
        {
            easynetQContainer.Register<Func<int>>(sp => () => 1);
            easynetQContainer.Register<Func<int>>(sp => () => 2);

            Assert.Equal(1, easynetQContainer.Resolve<Func<int>>()());
        }

        public void Dispose()
        {
            if (bus != null)
            {
                bus.Dispose();
                ((StructureMapAdapter)bus.Advanced.Container).Dispose();
            }
            RabbitHutch.SetContainerFactory(() => new DefaultServiceProvider());
        }

        public interface IComponent { }
        public class FirstComponent : IComponent { }
        public class SecondComponent : IComponent { }

        private int One()
        {
            return 1;
        }

        private int Two()
        {
            return 2;
        }
    }
}
