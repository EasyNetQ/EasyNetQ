namespace EasyNetQ;

/// <summary>
///     Registers message-type descriptors into the <see cref="IMessageTypeRegistry" /> when it is constructed.
///     Implementations are emitted by the source generator with closed generic <c>GetOrAdd&lt;T&gt;()</c> calls,
///     which keeps every descriptor visible to the AOT compiler and leaves no work for runtime reflection.
/// </summary>
public interface IMessageTypeRegistryInitializer
{
    /// <summary>
    ///     Called once while the registry is being constructed.
    /// </summary>
    void Initialize(IMessageTypeRegistry registry);
}
