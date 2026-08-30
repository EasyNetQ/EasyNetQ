namespace EasyNetQ.AllocationTests;

/// <summary>
///     Bytes allocated per iteration, as measured at the end of each phase. These are a ratchet: each phase may only
///     lower them. Phase history lives in Source/EasyNetQ.Benchmarks/results/.
/// </summary>
public static class Ceilings
{
    // Phase 0 baseline (8.x pipeline, .NET 10, arm64). Handlers are no-ops returning a cached task, so consume
    // numbers are framework overhead + the deserialized message graph; publish numbers stop at BasicProperties.
    // Phase 1 (layered pipeline, pooled contexts): unchanged — the remaining bytes are Message<T> (144 B) and the
    // deserialized object / BasicProperties, which Phase 2 removes.
    public const long ConsumeSmall = 208;
    public const long ConsumeMedium = 1776;
    public const long ConsumeLarge = 13664;
    public const long PublishAdvancedSmall = 408;
    public const long PublishPubSubSmall = 464;
    public const long EventBusPublishNoSubscribers = 0;
    public const long EventBusPublishOneSubscriber = 0;

    // Phase 1 gates: the new plumbing itself must be allocation-free
    public const long PropertyBagGetSet = 0;
    public const long ContextInheritedGet = 0;
    public const long PipelineOverheadNoop = 0;
}
