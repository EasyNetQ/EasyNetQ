namespace EasyNetQ.Pipeline;

/// <summary>
///     A strongly typed key for a value stored in a <see cref="PropertyBag" /> / <see cref="IProperties" />.
///     Keys are compared by identity (a process-unique id assigned at construction), never by name, so two keys
///     with the same name are still different keys. Declare keys once as <c>static readonly</c> fields.
/// </summary>
/// <typeparam name="T">The type of the value the key refers to</typeparam>
public readonly struct PropertyKey<T> : IEquatable<PropertyKey<T>>
{
    /// <summary>
    ///     Creates a new, unique key
    /// </summary>
    /// <param name="name">A descriptive name, used for diagnostics only</param>
    public PropertyKey(string name)
    {
        Id = PropertyKeyIds.Next();
        Name = name;
    }

    /// <summary>
    ///     Process-unique identity of the key
    /// </summary>
    public int Id { get; }

    /// <summary>
    ///     Descriptive name of the key (diagnostics only)
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public bool Equals(PropertyKey<T> other) => Id == other.Id;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PropertyKey<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Id;

    /// <inheritdoc />
    public override string ToString() => Name;

    public static bool operator ==(PropertyKey<T> left, PropertyKey<T> right) => left.Equals(right);

    public static bool operator !=(PropertyKey<T> left, PropertyKey<T> right) => !left.Equals(right);
}

internal static class PropertyKeyIds
{
    private static int next;

    public static int Next() => Interlocked.Increment(ref next);
}
