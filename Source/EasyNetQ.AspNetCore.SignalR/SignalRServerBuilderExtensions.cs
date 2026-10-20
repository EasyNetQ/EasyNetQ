using EasyNetQ.AspNetCore.SignalR;
using EasyNetQ.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the EasyNetQ SignalR backplane</summary>
public static class EasyNetQSignalRServerBuilderExtensions
{
    /// <summary>
    ///     Scales SignalR out over the application's EasyNetQ transport (register it first, e.g.
    ///     <c>services.AddEasyNetQ("host=rabbitmq")</c>): every hub gets a <see cref="EasyNetQHubLifetimeManager{THub}" />.
    /// </summary>
    public static ISignalRServerBuilder AddEasyNetQ(this ISignalRServerBuilder signalr, Action<EasyNetQBackplaneConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(signalr);
        var configurator = new EasyNetQBackplaneConfigurator();
        configure?.Invoke(configurator);

        signalr.Services.AddSingleton(configurator.Build());
        signalr.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(EasyNetQHubLifetimeManager<>));
        // once however often AddEasyNetQ is called (TryAddEnumerable would also skip it next to the app's own
        // lifecycle configurations, which share the type): redeclares each hub's queue after a recovery
        if (!signalr.Services.Any(d => ReferenceEquals(d.ImplementationInstance, BackplaneLifecycle.Configuration)))
            signalr.Services.AddSingleton(BackplaneLifecycle.Configuration);
        return signalr;
    }
}

