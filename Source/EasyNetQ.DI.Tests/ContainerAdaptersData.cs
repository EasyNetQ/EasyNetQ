using Autofac;
using Castle.Windsor;
using EasyNetQ.DI.Autofac;
using EasyNetQ.DI.Microsoft;
using EasyNetQ.DI.Ninject;
using EasyNetQ.DI.Windsor;
using EasyNetQ.LightInject;
using LightInject;
using Microsoft.Extensions.DependencyInjection;
using Ninject;
using System.Collections;
using System.Collections.Generic;

namespace EasyNetQ.DI.Tests;

internal class ContainerAdaptersData : IEnumerable<object[]>
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
            "Autofac",
            (ResolverFactory)(c =>
            {
                var containerBuilder = new ContainerBuilder();
                var adapter = new AutofacAdapter(containerBuilder);
                c(adapter);
                var container = containerBuilder.Build();
                return container.Resolve<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "Windsor",
            (ResolverFactory)(c =>
            {
                var container = new WindsorContainer();
                var adapter = new WindsorAdapter(container);
                c(adapter);
                return container.Resolve<IServiceResolver>();
            })
        };

        yield return new object[]
        {
            "Ninject",
            (ResolverFactory)(c =>
            {
                var container = new StandardKernel();
                var adapter = new NinjectAdapter(container);
                c(adapter);
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
                c(adapter);
                var serviceProvider = serviceCollection.BuildServiceProvider(true); //validate scopes
                return serviceProvider.GetService<IServiceResolver>();
            })
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

