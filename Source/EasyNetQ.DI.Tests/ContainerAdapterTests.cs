using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using Castle.Windsor;
using EasyNetQ.DI.Autofac;
using EasyNetQ.DI.LightInject;
using EasyNetQ.DI.Microsoft;
using EasyNetQ.DI.Ninject;
using EasyNetQ.DI.SimpleInjector;
using EasyNetQ.DI.StructureMap;
using EasyNetQ.DI.Windsor;
using EasyNetQ.Logging;
using LightInject;
using Ninject;
using Xunit;
using LightInjectContainer = LightInject.ServiceContainer;
using NinjectContainer = Ninject.StandardKernel;
using SimpleInjectorContainer = SimpleInjector.Container;
using StructureMapContainer = StructureMap.Container;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace EasyNetQ.DI.Tests;

public class ContainerAdapterTests
{
    public delegate IServiceResolver ResolverFactory(Action<IServiceRegister, ICollectionServiceRegister> configure);

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_last_registration_win(string name, ResolverFactory resolverFactory)
    {
        var first = new Service();
        var last = new Service();

        var resolver = resolverFactory((c, _) =>
        {
            c.Register<IService>(first);
            c.Register<IService>(last);
        });

        Assert.Equal(last, resolver.Resolve<IService>());
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_resolve_single_registration(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((c, collection) =>
        {
            // override registrations
            c.Register<IService, Service>();
            c.Register<IService, Service2>();
            c.Register<IService, Service3>();
            c.Register<IServiceWithCollection, ServiceWithCollection>();
        });

        var serviceWithCollection = resolver.Resolve<IServiceWithCollection>();
        Assert.Single(serviceWithCollection.Services);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_resolve_multiple_registrations(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((c, collection) =>
        {
            // append registrations
            collection.Register<IService, Service>();
            collection.Register<IService, Service2>();
            collection.Register<IService, Service3>();
            c.Register<IServiceWithCollection, ServiceWithCollection>();
        });

        var serviceWithCollection = resolver.Resolve<IServiceWithCollection>();
        Assert.Equal(3, serviceWithCollection.Services.Length);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_singleton_created_once(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((c, _) => c.Register<IService, Service>());

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        Assert.Same(first, second);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_transient_created_every_time(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((c, _) => c.Register<IService, Service>(Lifetime.Transient));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        Assert.NotSame(first, second);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_resolve_service_resolver(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((_, _) => { });

        Assert.NotNull(resolver.Resolve<IServiceResolver>());
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_singleton_factory_called_once(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((c, _) => c.Register<IService>(_ => new Service()));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        Assert.Same(first, second);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_transient_factory_call_every_time(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((c, _) => c.Register<IService>(_ => new Service(), Lifetime.Transient));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        Assert.NotSame(first, second);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_override_dependency_with_factory(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((c, _) => c.Register<IService, Service>().Register(_ => (IService)new DummyService()));
        Assert.IsType<DummyService>(resolver.Resolve<IService>());
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_resolve_singleton_generic(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory((c, _) => c.Register(typeof(ILogger<>), typeof(NoopLogger<>)));
        var intLogger = resolver.Resolve<ILogger<int>>();
        var floatLogger = resolver.Resolve<ILogger<float>>();

        Assert.IsType<NoopLogger<int>>(intLogger);
        Assert.IsType<NoopLogger<float>>(floatLogger);

        Assert.Same(intLogger, resolver.Resolve<ILogger<int>>());
        Assert.Same(floatLogger, resolver.Resolve<ILogger<float>>());
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_resolve_transient_generic(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(
            (c, _) => c.Register(typeof(ILogger<>), typeof(NoopLogger<>), Lifetime.Transient)
        );

        var intLogger = resolver.Resolve<ILogger<int>>();
        var floatLogger = resolver.Resolve<ILogger<float>>();

        Assert.IsType<NoopLogger<int>>(intLogger);
        Assert.IsType<NoopLogger<float>>(floatLogger);

        Assert.NotSame(intLogger, resolver.Resolve<ILogger<int>>());
        Assert.NotSame(floatLogger, resolver.Resolve<ILogger<float>>());
    }

    public static IEnumerable<object[]> GetContainerAdapters()
    {
        yield return new object[]
        {
            "Default",
            (ResolverFactory)(c =>
            {
                var container = new DefaultServiceContainer();
                c(container, container);
                return container.Resolve<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "LightInject",
            (ResolverFactory)(c =>
            {
                var container = new LightInjectContainer();
                var adapter = new LightInjectAdapter(container);
                c(adapter, adapter);
                return container.GetInstance<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "SimpleInjector",
            (ResolverFactory)(c =>
            {
                var container = new SimpleInjectorContainer { Options = { AllowOverridingRegistrations = true } };
                 var adapter = new SimpleInjectorAdapter(container);
                c(adapter, adapter);
                return container.GetInstance<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "StructureMap",
            (ResolverFactory)(c =>
            {
                var container = new StructureMapContainer(r =>
                {
                    var adapter = new StructureMapAdapter(r);
                    c(adapter, adapter);
                });
                return container.GetInstance<IServiceResolver>();
            })
        };

        yield return new object[]
        {
             "Autofac",
            (ResolverFactory)(c =>
            {
                var containerBuilder = new ContainerBuilder();
                var adapter = new AutofacAdapter(containerBuilder);
                c(adapter, adapter);
                var container = containerBuilder.Build();
                return container.Resolve<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "Castle.Windsor",
            (ResolverFactory)(c =>
            {
                var container = new WindsorContainer();
                container.Kernel.Resolver.AddSubResolver(new Castle.MicroKernel.Resolvers.SpecializedResolvers.CollectionResolver(container.Kernel));
                var adapter = new WindsorAdapter(container);
                c(adapter, adapter);
                return container.Resolve<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "Ninject",
            (ResolverFactory)(c =>
            {
                var container = new NinjectContainer();
                var adapter = new NinjectAdapter(container);
                c(adapter, adapter);
                return container.Get<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "ServiceCollection",
            (ResolverFactory)(c =>
            {
                var serviceCollection = new ServiceCollection();
                var adapter = new ServiceCollectionAdapter(serviceCollection);
                c(adapter, adapter);
                var serviceProvider = serviceCollection.BuildServiceProvider(true); //validate scopes
                return serviceProvider.GetService<IServiceResolver>();
            })
        };
    }

    public interface IService
    {
    }

    public class DummyService : IService
    {
    }

    public class Service : IService
    {
        private static volatile int sequenceNumber;

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

    public class Service2 : IService
    {
    }

    public class Service3 : IService
    {
    }

    public interface IServiceWithCollection
    {
        IService[] Services { get; }
    }

    public class ServiceWithCollection : IServiceWithCollection
    {
        public ServiceWithCollection(IEnumerable<IService> services)
        {
            Services = services.ToArray();
        }

        public IService[] Services { get; }
    }
}
