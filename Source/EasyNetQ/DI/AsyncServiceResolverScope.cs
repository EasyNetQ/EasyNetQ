namespace EasyNetQ.DI;

/// <summary>
/// An <see cref="IServiceResolverScope" /> implementation that implements <see cref="IAsyncDisposable" />.
/// </summary>
public readonly struct AsyncServiceResolverScope : IServiceResolverScope, IAsyncDisposable
{
    private readonly IServiceResolverScope _serviceResolverScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncServiceResolverScope"/> struct.
    /// Wraps an instance of <see cref="IServiceResolverScope" />.
    /// </summary>
    /// <param name="serviceResolverScope">The <see cref="IServiceResolverScope"/> instance to wrap.</param>
    public AsyncServiceResolverScope(IServiceResolverScope serviceResolverScope)
    {
        _serviceResolverScope = serviceResolverScope;
    }

    public IServiceResolverScope CreateScope() => new AsyncServiceResolverScope(_serviceResolverScope.CreateScope());

    public TService Resolve<TService>() where TService : class => _serviceResolverScope.Resolve<TService>();

    /// <inheritdoc />
    public void Dispose()
    {
        _serviceResolverScope.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_serviceResolverScope is IAsyncDisposable ad)
        {
            return ad.DisposeAsync();
        }
        _serviceResolverScope.Dispose();

        // ValueTask.CompletedTask is only available in net5.0 and later.
        return default;
    }
}
