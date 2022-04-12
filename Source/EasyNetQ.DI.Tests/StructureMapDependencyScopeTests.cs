using System;
using EasyNetQ.DI.StructureMap;
using Shouldly;
using StructureMap;
using Xunit;

namespace EasyNetQ.DI.Tests;

public class StructureMapDependencyScopeTests
{
    [Theory]
    [InlineData(Lifetime.Transient, true)]
    [InlineData(Lifetime.Singleton, false)]
    public void CreateScope_TransientService_ShouldBeDisposed(Lifetime lifetime, bool shouldBeDisposed)
    {
        var resolver = CreateResolver(c => c.Register<IService, Service>(lifetime));

        var service = ResolveFromScope(resolver);

        service.Disposed.ShouldBe(shouldBeDisposed);
    }

    [Fact]
    public void CreateScope_TransientServiceInSameScope_ShouldBeSingleton()
    {
        var resolver = CreateResolver(c => c.Register<IService, Service>(Lifetime.Transient));

        IService service1;
        IService service2;
        using (var scope = resolver.CreateScope())
        {
            service1 = scope.Resolve<IService>();
            service2 = scope.Resolve<IService>();
        }

        service2.ShouldBe(service1);
    }

    [Fact]
    public void CreateScope_TransientServiceInDifferentScopes_ShouldNotBeSame()
    {
        var resolver = CreateResolver(c => c.Register<IService, Service>(Lifetime.Transient));

        var service1 = ResolveFromScope(resolver);
        var service2 = ResolveFromScope(resolver);

        service2.ShouldNotBe(service1);
    }

    private static IService ResolveFromScope(IServiceResolver resolver)
    {
        using (var scope = resolver.CreateScope())
        {
            return scope.Resolve<IService>();
        }
    }

    private IServiceResolver CreateResolver(Action<IServiceRegister> configure)
    {
        var container = new Container(r => configure(new StructureMapAdapter(r)));
        return container.GetInstance<IServiceResolver>();
    }

    private interface IService : IDisposable
    {
        bool Disposed { get; set; }
    }

    private class Service : IService
    {
        public bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
