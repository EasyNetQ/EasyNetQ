﻿using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using EasyNetQ.DI.Autofac;
using EasyNetQ.DI.LightInject;
using EasyNetQ.DI.SimpleInjector;
using EasyNetQ.DI.StructureMap;
using EasyNetQ.DI.Windsor;
using EasyNetQ.DI.Ninject;
using Ninject;
using Xunit;
using LightInjectContainer = LightInject.ServiceContainer;
using SimpleInjectorContainer = SimpleInjector.Container;
using StructureMapContainer = StructureMap.Container;
using WindsorContainer = Castle.Windsor.WindsorContainer;
using NinjectContainer = Ninject.StandardKernel;
#if !NETFX
using EasyNetQ.DI.Microsoft;
using Microsoft.Extensions.DependencyInjection;
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
            
            var resolver = resolverFactory(c =>
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
            var resolver = resolverFactory(c => c.Register<IService, Service>());

            var first = resolver.Resolve<IService>();
            var second = resolver.Resolve<IService>();
            
            Assert.Same(first, second);
        }

        [Theory]
        [MemberData(nameof(GetContainerAdapters))]
        public void Should_transient_created_every_time(ResolverFactory resolverFactory)
        {
            var resolver = resolverFactory(c => c.Register<IService, Service>(Lifetime.Transient));

            var first = resolver.Resolve<IService>();
            var second = resolver.Resolve<IService>();

            Assert.NotSame(first, second);
        }

        [Theory]
        [MemberData(nameof(GetContainerAdapters))]
        public void Should_resolve_service_resolver(ResolverFactory resolverFactory)
        {
            var resolver = resolverFactory(c => {});

            Assert.NotNull(resolver.Resolve<IServiceResolver>());
        }

        [Theory]
        [MemberData(nameof(GetContainerAdapters))]
        public void Should_singleton_factory_called_once(ResolverFactory resolverFactory)
        {
            var resolver = resolverFactory(c => c.Register<IService>(x => new Service()));

            var first = resolver.Resolve<IService>();
            var second = resolver.Resolve<IService>();

            Assert.Same(first, second);
        }

        [Theory]
        [MemberData(nameof(GetContainerAdapters))]
        public void Should_transient_factory_call_every_time(ResolverFactory resolverFactory)
        {
            var resolver = resolverFactory(c => c.Register<IService>(x => new Service(), Lifetime.Transient));

            var first = resolver.Resolve<IService>();
            var second = resolver.Resolve<IService>();

            Assert.NotSame(first, second);
        }

        public static IEnumerable<object[]> GetContainerAdapters()
        {
            yield return new object[] {(ResolverFactory) (c =>
            {
                var container = new DefaultServiceContainer();
                c(container);
                return container.Resolve<IServiceResolver>();
            })};

            yield return new object[] {(ResolverFactory) (c =>
            {
                var container = new LightInjectContainer();
                c(new LightInjectAdapter(container));
                return container.GetInstance<IServiceResolver>();
            })};

            yield return new object[] {(ResolverFactory) (c =>
            {
                var container = new SimpleInjectorContainer { Options = { AllowOverridingRegistrations = true } };
                c(new SimpleInjectorAdapter(container));
                return container.GetInstance<IServiceResolver>();
            })};

            yield return new object[] {(ResolverFactory) (c =>
            {
                var container = new StructureMapContainer(r => c(new StructureMapAdapter(r)));
                return container.GetInstance<IServiceResolver>();
            })};

            yield return new object[] {(ResolverFactory) (c =>
            {
                var containerBuilder = new ContainerBuilder();
                c(new AutofacAdapter(containerBuilder));
                var container = containerBuilder.Build();
                return container.Resolve<IServiceResolver>();
            })};
            
            yield return new object[] {(ResolverFactory) (c =>
            {
                var container = new WindsorContainer();
                c(new WindsorAdapter(container));
                return container.Resolve<IServiceResolver>();
            })};
            
            yield return new object[] {(ResolverFactory) (c =>
            {
                var container = new NinjectContainer();
                c(new NinjectAdapter(container));
                return container.Get<IServiceResolver>();
            })};

#if !NETFX
            yield return new object[] {(ResolverFactory) (c =>
            {
                var serviceCollection = new ServiceCollection();
                c(new ServiceCollectionAdapter(serviceCollection));
                var serviceProvider = serviceCollection.BuildServiceProvider();
                return serviceProvider.GetService<IServiceResolver>();
            })};
#endif
        }

        public interface IService
        {
        }

        public class Service : IService
        {
            private static volatile int sequenceNumber = 0;

            private readonly int number;
            
            public Service()
            {
                number = Interlocked.Increment(ref sequenceNumber);
            }

            public override string ToString()
            {
                return number.ToString();
            }
        }
    }
}