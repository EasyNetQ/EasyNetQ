#if !NET
// extensions live in the BCL namespace so netstandard2.0 call sites need no extra using
namespace System.Collections.Generic;

/// <summary>
///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
///     the same compatibility as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new EasyNetQ release.
/// </summary>
internal static class NetstandardPolyfills
{
    /// <summary>
    ///     netstandard2.0 counterpart of Dictionary&lt;TKey, TValue&gt;.TryAdd
    /// </summary>
    public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        where TKey : notnull
    {
        if (dictionary.ContainsKey(key))
            return false;
        dictionary.Add(key, value);
        return true;
    }

    /// <summary>
    ///     netstandard2.0 counterpart of KeyValuePair.Deconstruct
    /// </summary>
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
#endif
