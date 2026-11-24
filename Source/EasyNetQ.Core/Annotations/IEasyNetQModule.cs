using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

/// <summary>
///     A unit of compile-time-generated EasyNetQ registrations: message-type registry initializers, serializer
///     contexts and handler wiring. The source generator emits one module per assembly and registers it with the
///     bus builder; <see cref="EasyNetQBuilderModuleExtensions.AddModule" /> is the manual fallback for hosts the
///     generator cannot intercept.
/// </summary>
public interface IEasyNetQModule
{
    /// <summary>
    ///     Adds this module's registrations to the service collection. Implementations must be idempotent -
    ///     the same module may be added through an interceptor and through a referencing assembly's module list.
    /// </summary>
    void Register(IServiceCollection services);
}

/// <summary>
///     Module registration entry point on the bus builder.
/// </summary>
public static class EasyNetQBuilderModuleExtensions
{
    /// <summary>
    ///     Registers a generated module. Adding the same module type twice is a no-op.
    /// </summary>
    public static IEasyNetQBuilder AddModule<TModule>(this IEasyNetQBuilder builder, TModule module)
        where TModule : IEasyNetQModule
    {
        foreach (var descriptor in builder.Services)
        {
            if (descriptor.ServiceType == typeof(IEasyNetQModule) && descriptor.ImplementationInstance?.GetType() == module.GetType())
                return builder;
        }

        builder.Services.AddSingleton<IEasyNetQModule>(module);
        module.Register(builder.Services);
        return builder;
    }
}
