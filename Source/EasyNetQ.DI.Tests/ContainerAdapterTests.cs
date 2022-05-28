using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using EasyNetQ.DI.Autofac;
using EasyNetQ.DI.LightInject;
using EasyNetQ.DI.Microsoft;
using EasyNetQ.DI.Ninject;
using EasyNetQ.DI.Windsor;
using EasyNetQ.Logging;
using LightInject;
using Ninject;
using Xunit;
using LightInjectContainer = LightInject.ServiceContainer;
using NinjectContainer = Ninject.StandardKernel;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.DI.Tests;

public class ContainerAdapterTests
{
    public delegate IServiceResolver ResolverFactory(Action<IServiceRegister> configure);

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_first_type_registration_win(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c =>
        {
            c.Register<IService, NewService>();
            c.Register<IService, DefaultService>();
        });

        Assert.IsType<NewService>(resolver.Resolve<IService>());

        // To ensure that container doesn't know any other implementations
        Assert.Single(resolver.Resolve<IEnumerable<IService>>(), x => x is NewService);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_be_able_to_register_enumerable(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c =>
        {
            c.Register<IService, NewService>();
        });

        Assert.IsType<NewService>(resolver.Resolve<IService>());

        // To ensure that container doesn't know any other implementations
        Assert.Single(resolver.Resolve<IEnumerable<IService>>(), x => x is NewService);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_first_instance_registration_win(ResolverFactory resolverFactory)
    {
        var first = new NewService();
        var last = new NewService();

        var resolver = resolverFactory(c =>
        {
            c.Register<IService>(first);
            c.Register<IService>(last);
        });

        Assert.Equal(first, resolver.Resolve<IService>());

        // To ensure that container doesn't know any other implementations
        Assert.Single(resolver.Resolve<IEnumerable<IService>>(), x => x == first);
    }


    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_singleton_created_once(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register<IService, NewService>());

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        Assert.Same(first, second);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_transient_created_every_time(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register<IService, NewService>(Lifetime.Transient));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        Assert.NotSame(first, second);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_resolve_service_resolver(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(_ => { });

        Assert.NotNull(resolver.Resolve<IServiceResolver>());
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_singleton_factory_called_once(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register<IService>(_ => new NewService()));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        Assert.Same(first, second);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_transient_factory_call_every_time(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register<IService>(_ => new NewService(), Lifetime.Transient));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        Assert.NotSame(first, second);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_override_dependency_with_factory(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register(_ => (IService)new NewService()).Register<IService, DefaultService>());

        Assert.IsType<NewService>(resolver.Resolve<IService>());

        Assert.Single(resolver.Resolve<IEnumerable<IService>>(), x => x is NewService);
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_resolve_singleton_generic(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register(typeof(ILogger<>), typeof(NoopLogger<>)));

        var intLogger = resolver.Resolve<ILogger<int>>();
        var floatLogger = resolver.Resolve<ILogger<float>>();

        Assert.IsType<NoopLogger<int>>(intLogger);
        Assert.IsType<NoopLogger<float>>(floatLogger);

        Assert.Same(intLogger, resolver.Resolve<ILogger<int>>());
        Assert.Same(floatLogger, resolver.Resolve<ILogger<float>>());
    }

    [Theory]
    [MemberData(nameof(GetContainerAdapters))]
    public void Should_resolve_transient_generic(ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(
            c => c.Register(typeof(ILogger<>), typeof(NoopLogger<>), Lifetime.Transient)
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
            (ResolverFactory)(c =>
            {
                var container = new DefaultServiceContainer();
                c(container);
                return container.Resolve<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            (ResolverFactory)(c =>
            {
                var container = new LightInjectContainer();
                c(new LightInjectAdapter(container));
                return container.GetInstance<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            (ResolverFactory)(c =>
            {
                var containerBuilder = new ContainerBuilder();
                c(new AutofacAdapter(containerBuilder));
                var container = containerBuilder.Build();
                return container.Resolve<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            (ResolverFactory)(c =>
            {
                var container = new WindsorContainer();
                container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel, true));

                c(new WindsorAdapter(container));
                return container.Resolve<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            (ResolverFactory)(c =>
            {
                var container = new NinjectContainer();
                c(new NinjectAdapter(container));
                return container.Get<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            (ResolverFactory)(c =>
            {
                var serviceCollection = new ServiceCollection();
                c(new ServiceCollectionAdapter(serviceCollection));
                var serviceProvider = serviceCollection.BuildServiceProvider(true); //validate scopes
                return serviceProvider.GetService<IServiceResolver>();
            })
        };
    }

    public interface IService
    {
    }

    public class DefaultService : IService
    {
    }

    public class NewService : IService
    {
        private static volatile int sequenceNumber;

        private readonly int number;

        public NewService() => number = Interlocked.Increment(ref sequenceNumber);

        public override string ToString() => number.ToString();
    }
}
