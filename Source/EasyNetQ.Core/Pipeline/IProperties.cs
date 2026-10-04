namespace EasyNetQ.Pipeline;

/// <summary>
///     Read-only access to typed extension properties. This is the view a lower pipeline layer gets of a higher layer:
///     it can read inherited values but cannot change them.
/// </summary>
public interface IReadOnlyProperties
{
    /// <summary>
    ///     Gets the value stored for <paramref name="key" />, searching this layer first and then its parents
    /// </summary>
    bool TryGet<T>(PropertyKey<T> key, out T value);
}

/// <summary>
///     Read/write access to typed extension properties of the current layer
/// </summary>
public interface IProperties : IReadOnlyProperties
{
    /// <summary>
    ///     Stores <paramref name="value" /> for <paramref name="key" /> on this layer, shadowing any inherited value
    /// </summary>
    void Set<T>(PropertyKey<T> key, T value);

    /// <summary>
    ///     Removes the value stored for <paramref name="key" /> on this layer (inherited values are unaffected)
    /// </summary>
    bool Remove<T>(PropertyKey<T> key);
}

/// <summary>
///     Convenience accessors for <see cref="IReadOnlyProperties" />
/// </summary>
public static class PropertiesExtensions
{
    /// <summary>
    ///     Gets the value for <paramref name="key" /> or throws <see cref="KeyNotFoundException" />
    /// </summary>
    public static T Get<T>(this IReadOnlyProperties properties, PropertyKey<T> key)
        => properties.TryGet(key, out var value) ? value : throw new KeyNotFoundException($"Property '{key.Name}' is not set");

    /// <summary>
    ///     Gets the value for <paramref name="key" /> or <paramref name="fallback" /> when it is not set
    /// </summary>
    public static T GetOrDefault<T>(this IReadOnlyProperties properties, PropertyKey<T> key, T fallback = default!)
        => properties.TryGet(key, out var value) ? value : fallback;

    /// <summary>
    ///     Returns whether a value is set for <paramref name="key" /> on this layer or any parent
    /// </summary>
    public static bool Contains<T>(this IReadOnlyProperties properties, PropertyKey<T> key) => properties.TryGet(key, out _);
}
