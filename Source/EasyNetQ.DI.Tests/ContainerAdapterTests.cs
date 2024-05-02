using MS = Microsoft.Extensions.Logging;
namespace EasyNetQ.DI.Tests;

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters

public class ContainerAdapterTests
{
    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_last_registration_win_instance(string name, ResolverFactory resolverFactory)
    {
        var first = new Service();
        var last = new Service();

        var resolver = resolverFactory(c =>
        {
            c.Register<IService>(first);
            c.Register<IService>(last);
        });

        resolver.Resolve<IService>().Should().Be(last);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_last_registration_win_type(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c =>
        {
            c.Register<IService, Service>();
            c.Register<IService, DummyService>();
        });

        resolver.Resolve<IService>().Should().BeOfType<DummyService>();
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_last_registration_win_factory(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c =>
        {
            c.Register<IService, Service>(_ => new Service());
            c.Register<IService, DummyService>(_ => new DummyService());
        });

        resolver.Resolve<IService>().Should().BeOfType<DummyService>();
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_first_registration_win_instance(string name, ResolverFactory resolverFactory)
    {
        var first = new Service();
        var last = new Service();

        var resolver = resolverFactory(c =>
        {
            c.Register<IService>(first);
            c.TryRegister<IService>(last);
        });

        resolver.Resolve<IService>().Should().Be(first);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_first_registration_win_type(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c =>
        {
            c.Register<IService, Service>();
            c.TryRegister<IService, DummyService>();
        });

        resolver.Resolve<IService>().Should().BeOfType<Service>();
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_first_registration_win_factory(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c =>
        {
            c.Register<IService, Service>(_ => new Service());
            c.TryRegister<IService, DummyService>(_ => new DummyService());
        });

        resolver.Resolve<IService>().Should().BeOfType<Service>();
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_allow_multiple_try_register_instance(string name, ResolverFactory resolverFactory)
    {
        var first = new Service();
        var last = new Service();

        var resolver = resolverFactory(c =>
        {
            c.TryRegister<IService>(first);
            c.TryRegister<IService>(last);
            c.TryRegister<IService>(last);
        });

        resolver.Resolve<IService>().Should().Be(first);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_allow_multiple_try_register_type(string name, ResolverFactory resolverFactory)
    {
        var first = new Service();
        var last = new Service();

        var resolver = resolverFactory(c =>
        {
            c.TryRegister<IService>(first);
            c.TryRegister<IService>(last);
        });

        resolver.Resolve<IService>().Should().Be(first);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_allow_multiple_try_register_factory(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c =>
        {
            c.TryRegister<IService, Service>();
            c.TryRegister<IService, DummyService>();
        });

        resolver.Resolve<IService>().Should().BeOfType<Service>();
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_resolve_single_registration(string name, ResolverFactory resolverFactory)
    {
        if (name == "Autofac")
            return; // Autofac doesn't support replace mechanics, only full recreation of a container builder

        var resolver = resolverFactory(c =>
        {
            c.Register<IService, Service>();
            c.Register<IService, Service2>();
            c.Register<IService, Service3>();
            c.Register<IServiceWithCollection, ServiceWithCollection>();
        });
        var serviceWithCollection = resolver.Resolve<IServiceWithCollection>();
        serviceWithCollection.Services.Length.Should().Be(1);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_singleton_created_once(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register<IService, Service>());

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        second.Should().Be(first);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_transient_created_every_time(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register<IService, Service>(Lifetime.Transient));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        second.Should().NotBe(first);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_resolve_service_resolver(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(_ => { });

        resolver.Resolve<IServiceResolver>().Should().NotBeNull();
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_singleton_factory_called_once(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register<IService>(_ => new Service()));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        second.Should().Be(first);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_transient_factory_call_every_time(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register<IService>(_ => new Service(), Lifetime.Transient));

        var first = resolver.Resolve<IService>();
        var second = resolver.Resolve<IService>();

        second.Should().NotBe(first);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_override_dependency_with_factory(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c =>
        {
            c
            .Register<IService, Service>()
            .Register(_ => (IService)new DummyService());
        });
        resolver.Resolve<IService>().Should().BeOfType<DummyService>();
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_resolve_singleton_generic(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(c => c.Register(typeof(MS.ILogger<>), typeof(MS.Logger<>)));
        var intLogger = resolver.Resolve<MS.ILogger<int>>();
        var floatLogger = resolver.Resolve<MS.ILogger<float>>();

        intLogger.Should().BeOfType<MS.Logger<int>>();
        floatLogger.Should().BeOfType<MS.Logger<float>>();

        resolver.Resolve<MS.ILogger<int>>().Should().Be(intLogger);
        resolver.Resolve<MS.ILogger<float>>().Should().Be(floatLogger);
    }

    [Theory]
    [ClassData(typeof(ContainerAdaptersData))]
    public void Should_resolve_transient_generic(string name, ResolverFactory resolverFactory)
    {
        var resolver = resolverFactory(
            c => c.Register(typeof(MS.ILogger<>), typeof(MS.Logger<>), Lifetime.Transient)
        );

        var intLogger = resolver.Resolve<MS.ILogger<int>>();
        var floatLogger = resolver.Resolve<MS.ILogger<float>>();

        intLogger.Should().BeOfType<MS.Logger<int>>();
        floatLogger.Should().BeOfType<MS.Logger<float>>();

        resolver.Resolve<MS.ILogger<int>>().Should().NotBe(intLogger);
        resolver.Resolve<MS.ILogger<float>>().Should().NotBe(floatLogger);
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
            return GetType().Name + "_" + number;
        }
    }


    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public class Service2 : IService
    {
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public class Service3 : IService
    {
    }


    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public interface IServiceWithCollection
    {
        IService[] Services { get; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public class ServiceWithCollection : IServiceWithCollection
    {
        public ServiceWithCollection(IEnumerable<IService> services)
        {
            Services = services.ToArray();
        }

        public IService[] Services { get; }
    }
}
