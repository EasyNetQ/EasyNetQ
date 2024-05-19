using EasyNetQ.DI.Microsoft;
using EasyNetQ.LightInject;
using LightInject;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;

namespace EasyNetQ.DI.Tests;

internal sealed class ContainerAdaptersData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            "Default",
            (ResolverFactory)(c =>
            {
                var container = new EasyNetQ.LightInject.ServiceContainer(c => c.EnablePropertyInjection = false);
                var adapter = new LightInjectAdapter(container);
                c(adapter);
                return container.GetInstance<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "LightInject",
            (ResolverFactory)(c =>
            {
                var container = new global::LightInject.ServiceContainer(c => c.EnablePropertyInjection = false);
                var adapter = new LightInject.LightInjectAdapter(container);
                c(adapter);
                return container.GetInstance<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "ServiceCollection",
            (ResolverFactory)(c =>
            {
                var serviceCollection = new ServiceCollection();
                var adapter = new ServiceCollectionAdapter(serviceCollection);
                c(adapter);
                var serviceProvider = serviceCollection.BuildServiceProvider(true); //validate scopes
                return serviceProvider.GetService<IServiceResolver>();
            })
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

