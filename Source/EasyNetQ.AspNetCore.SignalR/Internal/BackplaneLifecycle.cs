using EasyNetQ.Pipeline;

namespace EasyNetQ.AspNetCore.SignalR.Internal;

internal static class BackplaneLifecycle
{
    public static readonly LifecycleConfiguration Configuration = new(
        builder => builder.Use("EasyNetQ.SignalR.Recovery", BackplaneRecovery.OnLifecycleAsync)
    );
}
