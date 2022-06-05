using System;
using System.Linq;
using Castle.MicroKernel.Handlers;
using Castle.MicroKernel.SubSystems.Naming;

namespace EasyNetQ.DI.Windsor;

/// <summary>
/// Allows to inspect and remove existing registrations.
/// https://github.com/castleproject/Windsor/issues/151
/// </summary>
public class RemovableNamingSubSystem : DefaultNamingSubSystem
{
    public RemovableNamingSubSystem(INamingSubSystem namingSubSystem)
    {
        foreach (var handler in namingSubSystem.GetAllHandlers())
            Register(handler);
    }

    public void RemoveHandler(Type serviceType)
    {
        var invalidate = false;
        using (@lock.ForWriting())
        {
            if (name2Handler.ContainsKey(serviceType.FullName))
            {
                invalidate = true;
                name2Handler.Remove(serviceType.FullName);
            }

            if (service2Handler.ContainsKey(serviceType))
            {
                invalidate = true;
                service2Handler.Remove(serviceType);
            }

            if (invalidate)
                InvalidateCache();
        }
    }

    public bool HasComponentWithImplementation(Type serviceType, Type implementationType)
    {
        using (@lock.ForReading())
        {
            foreach (var handler in name2Handler.Values)
            {
                if (handler is AbstractHandler h && h.ComponentModel.Implementation == implementationType && h.ComponentModel.Services.Contains(serviceType))
                    return true;
            }
        }

        return false;
    }
}
