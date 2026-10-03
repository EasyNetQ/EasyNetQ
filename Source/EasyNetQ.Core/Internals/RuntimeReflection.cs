using System.Diagnostics.CodeAnalysis;

namespace EasyNetQ.Internals;

/// <summary>
///     Guards the runtime-reflection fallbacks (loading a type from its wire name, closing a descriptor over a runtime
///     type, reflection-based JSON contracts). Call sites test
///     <c>RuntimeFeature.IsDynamicCodeSupported &amp;&amp; IsSupported</c> inline: Native AOT folds the first to false and
///     drops the reflection branch, the analyzers read the guard attributes. The AppContext switch turns it off
///     elsewhere (e.g. trimmed JIT apps).
/// </summary>
internal static class RuntimeReflection
{
    public const string SwitchName = "EasyNetQ.RuntimeReflection.IsSupported";

#if NET9_0_OR_GREATER
    [FeatureSwitchDefinition(SwitchName)]
    [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
    [FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
#endif
    public static bool IsSupported { get; } = !AppContext.TryGetSwitch(SwitchName, out var enabled) || enabled;

    public static EasyNetQException Unavailable(string what)
        => new($"{what} needs runtime reflection, which is unavailable here (Native AOT, trimming or {SwitchName}=false). Register the type (source generator, MessageType<T>()) or pass a JsonSerializerContext.");
}
