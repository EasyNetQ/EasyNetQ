using System;
using Castle.MicroKernel;
using Castle.MicroKernel.SubSystems.Naming;
using Castle.Windsor;

namespace EasyNetQ.DI.Windsor;

/// <summary>
/// Extension methods for <see cref="IWindsorContainer"/>.
/// https://github.com/castleproject/Windsor/issues/151
/// </summary>
public static class WindsorContainerExtensions
{
    public static IWindsorContainer RemoveHandler(this IWindsorContainer container, Type serviceType)
    {
        container.EnsureRemovableNamingSubSystem().RemoveHandler(serviceType);
        return container;
    }

    public static bool HasComponentWithImplementation(this IWindsorContainer container, Type serviceType, Type implementationType)
    {
        return container.EnsureRemovableNamingSubSystem().HasComponentWithImplementation(serviceType, implementationType);
    }

    private static RemovableNamingSubSystem EnsureRemovableNamingSubSystem(this IWindsorContainer container)
    {
        if (container.Kernel.GetSubSystem(SubSystemConstants.NamingKey) is not INamingSubSystem naming)
            throw new NotSupportedException();

        if (naming is RemovableNamingSubSystem removableNaming)
            return removableNaming;

        removableNaming = new RemovableNamingSubSystem(naming);
        container.Kernel.AddSubSystem(SubSystemConstants.NamingKey, removableNaming);
        return removableNaming;
    }
}
