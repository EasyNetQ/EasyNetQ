namespace EasyNetQ.Internals;

internal static class Compat
{
    // Reason shared by the 8.x-compatible reflection APIs; the v9 fluent API with generated registrations is the AOT path
    public const string ReflectionApi = "8.x-compatible reflection API: not trim- or Native-AOT-safe. Use the fluent v9 API (Consume/Publish, MessageType<T>) with generated registrations.";
    public const string Annotated = "The public entry point carries RequiresUnreferencedCode/RequiresDynamicCode.";
}
