namespace EasyNetQ.Pipeline;

/// <summary>
///     Root of the context hierarchy: one per logical connection
/// </summary>
public sealed class ConnectionContext : LayerContext, IConnectionView
{
    /// <summary>
    ///     Creates a connection context
    /// </summary>
    public ConnectionContext(string name, IServiceProvider services) : base(services)
    {
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }
}
