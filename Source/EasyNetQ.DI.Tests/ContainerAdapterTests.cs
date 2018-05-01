using System;
using System.Collections.Generic;
using Autofac;
using EasyNetQ.DI.Autofac;
using EasyNetQ.DI.LightInject;
using EasyNetQ.DI.SimpleInjector;
using EasyNetQ.DI.StructureMap;
using Xunit;
using LightInjectContainer = LightInject.ServiceContainer;
using SimpleInjectorContainer = SimpleInjector.Container;
using StructureMapContainer = StructureMap.Container;

#if NETFX
using EasyNetQ.DI.Windsor;
using EasyNetQ.DI.Ninject;
using WindsorContainer = Castle.Windsor.WindsorContainer;
using NinjectContainer = Ninject.StandardKernel;
#endif

namespace EasyNetQ.DI.Tests
{
    public class ContainerAdapterTests
    {
        public delegate IServiceResolver ResolverFactory(Action<IServiceRegister> configure);
        
        [Theory]
        [MemberData(nameof(GetContainerAdapters))]
        public void Should_last_registration_win(ResolverFactory resolverFactory)
        {
            var first = new Service();
            var last = new Service();
            
            var resolver = resolverFactory.Invoke(c =>
            {
                c.Register<IService>(first);
                c.Register<IService>(last);
            });
            
            Assert.Equal(last, resolver.Resolve<IService>());
        }
         
        [Theory]
        [MemberData(nameof(GetContainerAdapters))]
        public void Should_singleton_created_once(ResolverFactory resolverFactory)
        {
            var resolver = resolverFactory.Invoke(c => c.Register<IService, Service>());

            Assert.Same(resolver.Resolve<IService>(), resolver.Resolve<IService>());
        }

        [Theory]
        [MemberData(nameof(GetContainerAdapters))]
        public void Should_transient_created_every_time(ResolverFactory resolverFactory)
        {
            var resolver = resolverFactory.Invoke(c => c.Register<IService, Service>(Lifetime.Transient));

            Assert.NotSame(resolver.Resolve<IService>(), resolver.Resolve<IService>());
        }

        [Theory]
        [MemberData(nameof(GetContainerAdapters))]
        public void Should_resolve_service_resolver(ResolverFactory resolverFactory)
        {            
            var resolver = resolverFactory.Invoke(c => {});

            Assert.NotNull(resolver.Resolve<IServiceResolver>());
        }

        public static IEnumerable<object[]> GetContainerAdapters()
        {
            object[] T<TContainer>(TContainer container) where TContainer : IServiceRegister, IServiceResolver
            {
                return new object[] {(ResolverFactory) (c =>
                {
                    c(container);
                    return container;
                })};
            }

            yield return T(new DefaultServiceContainer());
            yield return T(new LightInjectAdapter(new LightInjectContainer()));
            yield return T(new SimpleInjectorAdapter(new SimpleInjectorContainer { Options = { AllowOverridingRegistrations = true } }));
            yield return T(new StructureMapAdapter(new StructureMapContainer()));
#if NETFX
            yield return T(new WindsorAdapter(new WindsorContainer()));
            yield return T(new NinjectAdapter(new NinjectContainer()));
#endif
            yield return new object[] {(ResolverFactory) (c =>
            {
                var containerBuilder = new ContainerBuilder();
                c(new AutofacAdapter(containerBuilder));
                var container = containerBuilder.Build();
                return container.Resolve<IServiceResolver>();
            })};
        }

        public interface IService
        {
        }

        public class Service : IService
        {
        }
    }
}