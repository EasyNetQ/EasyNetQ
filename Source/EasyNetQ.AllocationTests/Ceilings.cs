namespace EasyNetQ.AllocationTests;

/// <summary>
///     Bytes allocated per iteration, as measured at the end of each phase. These are a ratchet: each phase may only
///     lower them. Phase history lives in Source/EasyNetQ.Benchmarks/results/.
/// </summary>
public static class Ceilings
{
    // Phase 0 baseline (8.x pipeline, .NET 10, arm64). Handlers are no-ops, so consume numbers are framework
    // overhead + the deserialized message graph; publish numbers stop at BasicProperties.
    // Phase 1 (layered pipeline, pooled contexts): totals unchanged; plumbing gated at 0 B.
    // Phase 2 (message type registry, typed dispatch, generic serializer): Message<T> (144 B) gone from both paths —
    // consume small is now exactly the deserialized object (64 B); publish keeps ArrayPooledMemoryStream +
    // BasicProperties + the correlation-id string (transport-boundary costs, Phases 4/5 targets).
    public const long ConsumeSmall = 64;
    public const long ConsumeMedium = 1632;
    public const long ConsumeLarge = 13520;
    public const long PublishAdvancedSmall = 264;
    public const long PublishPubSubSmall = 320;
    public const long EventBusPublishNoSubscribers = 0;
    public const long EventBusPublishOneSubscriber = 0;

    // Phase 1 gates: the new plumbing itself must be allocation-free
    public const long PropertyBagGetSet = 0;
    public const long ContextInheritedGet = 0;
    public const long PipelineOverheadNoop = 0;
}
