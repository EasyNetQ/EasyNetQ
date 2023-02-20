namespace EasyNetQ.DI;

/// <summary>
/// An <see cref="IServiceResolverScope" /> implementation that implements <see cref="IAsyncDisposable" />.
/// </summary>
public readonly struct AsyncServiceResolverScope : IServiceResolverScope, IAsyncDisposable
{
    private readonly IServiceResolverScope serviceResolverScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncServiceResolverScope"/> struct.
    /// Wraps an instance of <see cref="IServiceResolverScope" />.
    /// </summary>
    /// <param name="serviceResolverScope">The <see cref="IServiceResolverScope"/> instance to wrap.</param>
    public AsyncServiceResolverScope(IServiceResolverScope serviceResolverScope)
        => this.serviceResolverScope = serviceResolverScope;

    public IServiceResolverScope CreateScope() => serviceResolverScope.CreateScope();

    public TService Resolve<TService>() where TService : class => serviceResolverScope.Resolve<TService>();

    /// <inheritdoc />
    public void Dispose() => serviceResolverScope.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (serviceResolverScope is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        serviceResolverScope.Dispose();
        return default;
    }
}
