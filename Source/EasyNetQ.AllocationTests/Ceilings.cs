namespace EasyNetQ.AllocationTests;

/// <summary>
///     Bytes allocated per iteration, as measured at the end of each phase. These are a ratchet: each phase may only
///     lower them. Phase history lives in Source/EasyNetQ.Benchmarks/results/.
/// </summary>
public static class Ceilings
{
    // Phase 0 baseline (8.x pipeline, .NET 10, arm64). Handlers are no-ops returning a cached task, so consume
    // numbers are framework overhead + the deserialized message graph; publish numbers stop at BasicProperties.
    public const long ConsumeSmall = 208;
    public const long ConsumeMedium = 1776;
    public const long ConsumeLarge = 13664;
    public const long PublishAdvancedSmall = 408;
    public const long PublishPubSubSmall = 464;
    public const long EventBusPublishNoSubscribers = 0;
    public const long EventBusPublishOneSubscriber = 0;
}
