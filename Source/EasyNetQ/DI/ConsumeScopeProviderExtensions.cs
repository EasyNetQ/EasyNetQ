namespace EasyNetQ.DI;

/// <summary>
/// Provides extension methods to work with <see cref="IConsumeScopeProvider"/>.
/// </summary>
public static class ConsumeScopeProviderExtensions
{
    /// <summary>
    /// Creates a new <see cref="AsyncServiceResolverScope"/> that can be used to resolve scoped services.
    /// </summary>
    /// <param name="provider">The <see cref="IConsumeScopeProvider"/> to create the scope from.</param>
    /// <returns>An <see cref="AsyncServiceResolverScope"/> that can be used to resolve scoped services.</returns>
    public static AsyncServiceResolverScope CreateAsyncScope(this IConsumeScopeProvider provider)
        => new(provider.CreateScope());
}
