namespace EasyNetQ;

/// <summary>
///     Maps CLR message types and wire type names to <see cref="MessageTypeDescriptor" />s. Registration through the
///     generic <see cref="GetOrAdd{T}" /> is reflection-free; the <see cref="Type" /> and wire-name lookups fall back
///     to a runtime resolver for types nothing has registered (that fallback moves out of the core with the source
///     generator).
/// </summary>
public interface IMessageTypeRegistry
{
    /// <summary>
    ///     Gets the descriptor for <typeparamref name="T" />, creating and caching it on first use
    /// </summary>
    MessageTypeDescriptor<T> GetOrAdd<T>();

    /// <summary>
    ///     Gets the descriptor for a runtime <see cref="Type" />, creating it via the runtime fallback when no
    ///     generic registration has happened for it yet
    /// </summary>
    MessageTypeDescriptor GetOrAdd(Type type);

    /// <summary>
    ///     Looks up a descriptor by wire type name; only names seen before (registered or resolved) match
    /// </summary>
    bool TryGetByWireName(string wireName, out MessageTypeDescriptor descriptor);

    /// <summary>
    ///     Gets the descriptor for a wire type name, resolving unknown names through the configured
    ///     <see cref="ITypeNameSerializer" /> and caching the result
    /// </summary>
    MessageTypeDescriptor GetByWireName(string wireName);
}
