using EasyNetQ.Examples.Soak.Scenarios;

namespace EasyNetQ.Examples.Soak;

/// <summary>
///     Every scenario, with whether it uses only Native-AOT-safe API (fluent configuration, IBus with generic
///     messages, interceptors, typed queue settings) or the 8.x reflection API (Newtonsoft, versioning, polymorphism,
///     AutoSubscriber)
/// </summary>
public static class ScenarioCatalog
{
    public static readonly IReadOnlyList<Scenario> All =
    [
        new("newtonsoft", false, "UseNewtonsoftJson round trip, TypeNameHandling.Auto polymorphic members, base-typed publish", SerializationScenarios.NewtonsoftAsync),
        new("interceptors", true, "GZip + TripleDES interceptors, custom compress step after SerializeStep, wire format, error-queue body", InterceptorScenarios.InterceptorsAsync),
        new("versioning", false, "UseVersionedMessage + ISupersede, old consumer gets newer versions, unknown-version fallback", TypingScenarios.VersioningAsync),
        new("polymorphism", false, "UseAdvancedMessagePolymorphism, interface subscriber gets every implementation", TypingScenarios.PolymorphismAsync),
        new("priority", true, "MaxPriority queues (PubSub + fluent), highest priority first", PublishScenarios.PriorityAsync),
        new("multichannel", true, "UseMultiChannelClientCommandDispatcher with confirms under concurrent publishers", PublishScenarios.MultiChannelAsync),
        new("confirms", true, "publisher confirms on/off/per request, mandatory returns, recovery after a channel error", PublishScenarios.ConfirmsAsync),
        new("scheduler", true, "FuturePublish via DLX+TTL and via the delayed-exchange plugin", MessagingScenarios.SchedulerAsync),
        new("rpc", true, "Rpc under concurrency, expiration, unanswered and faulted requests", MessagingScenarios.RpcAsync),
        new("sendreceive", true, "SendReceive with two message types on one queue", MessagingScenarios.SendReceiveAsync),
        new("autosubscriber", false, "reflection AutoSubscriber: IConsume/IConsumeAsync, [ForTopic], [SubscriptionConfiguration], [AutoSubscriberConsumer]", MessagingScenarios.AutoSubscriberAsync),
        new("cancellation", true, "queue deleted under PubSub and fluent consumers raises LifecycleEvent.Cancelled", ConsumerScenarios.CancellationAsync),
        new("concurrency", true, "consumerDispatcherConcurrency 1 vs 8: order, overlap, prefetch bounds", ConsumerScenarios.ConcurrencyAsync),
        new("errors", true, "default and named quorum error queues, Error envelope, LogFailedMessageBodies", ErrorScenarios.ErrorsAsync),
        new("queues", true, "typed queue settings: TTL/DLX/reject dead-lettering, x-expires, quorum, stream", QueueScenarios.QueuesAsync),
    ];
}
