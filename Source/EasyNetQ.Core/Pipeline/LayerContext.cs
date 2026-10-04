namespace EasyNetQ.Pipeline;

/// <summary>
///     Base of every pipeline context. A context is one layer in the hierarchy
///     connection → channel → consumer → message: it owns its own typed properties and can read, but never write,
///     the properties of the layers above it (they are exposed to it only through read-only views).
/// </summary>
/// <remarks>
///     Message-level contexts are pooled and reused by the transport; do not hold on to one after your middleware
///     or handler returns unless you call <see cref="Detach" />.
/// </remarks>
public abstract class LayerContext : IProperties
{
    private readonly LayerContext? parent;
    private PropertyBag bag;
    private IServiceProvider? services;
    private bool detached;

    /// <summary>
    ///     Creates a root context
    /// </summary>
    protected LayerContext(IServiceProvider services)
    {
        this.services = services;
    }

    /// <summary>
    ///     Creates a child context of <paramref name="parent" />
    /// </summary>
    protected LayerContext(LayerContext parent)
    {
        this.parent = parent;
    }

    /// <summary>
    ///     The layer above this one, read-only; <see langword="null" /> for the root
    /// </summary>
    public IReadOnlyProperties? Parent => parent;

    /// <summary>
    ///     Services available to this layer. Defaults to the parent's services; a scope middleware may replace it
    ///     for the duration of a message.
    /// </summary>
    public IServiceProvider Services
    {
        get => services ?? parent!.Services;
        set => services = value;
    }

    /// <summary>
    ///     <see langword="true" /> once <see cref="Detach" /> has been called; the context will not be returned to its pool
    /// </summary>
    public bool IsDetached => detached;

    /// <inheritdoc />
    public bool TryGet<T>(PropertyKey<T> key, out T value)
    {
        if (bag.TryGet(key, out value)) return true;
        if (parent is not null) return parent.TryGet(key, out value);
        value = default!;
        return false;
    }

    /// <summary>
    ///     Gets a value stored on this layer only, ignoring parents
    /// </summary>
    public bool TryGetLocal<T>(PropertyKey<T> key, out T value) => bag.TryGet(key, out value);

    /// <inheritdoc />
    public void Set<T>(PropertyKey<T> key, T value) => bag.Set(key, value);

    /// <inheritdoc />
    public bool Remove<T>(PropertyKey<T> key) => bag.Remove(key);

    /// <summary>
    ///     Opts this context out of pooling, e.g. because a handler stores it for later use
    /// </summary>
    public void Detach() => detached = true;

    /// <summary>
    ///     Clears per-use state so the context can be reused. Derived contexts must call the base implementation.
    /// </summary>
    protected internal virtual void Reset()
    {
        bag.Clear();
        if (parent is not null) services = null;
    }
}
