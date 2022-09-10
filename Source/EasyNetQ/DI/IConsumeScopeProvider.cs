using System;

namespace EasyNetQ.DI;

/// <summary>
/// Provides dependency resolution scope for <see cref="RabbitAdvancedBus.Consume(Action{IConsumeConfiguration})"/>
/// </summary>
public interface IConsumeScopeProvider
{
    /// <summary>
    /// Creates scope
    /// </summary>
    IServiceResolverScope CreateScope();
}

/// <summary>
/// Default scope provider that uses already registered <see cref="IServiceResolver"/>
/// </summary>
public class DefaultConsumeScopeProvider : IConsumeScopeProvider
{
    private readonly IServiceResolver resolver;

    /// <summary>
    /// Creates default provider
    /// </summary>
    public DefaultConsumeScopeProvider(IServiceResolver resolver) => this.resolver = resolver;

    /// <inheritdoc />
    public IServiceResolverScope CreateScope() => resolver.CreateScope();
}

/// <summary>
/// Noop scope provider used for backward compatibility especially for <see cref="AutoSubscribe.AutoSubscriber"/>
/// </summary>
public class NoopConsumeScopeProvider : IConsumeScopeProvider
{
    private static readonly IServiceResolverScope Scope = new NoopDisposable();

    private sealed class NoopDisposable : IServiceResolverScope
    {
        public IServiceResolverScope CreateScope() => this;

        public void Dispose() { }

        public TService Resolve<TService>() where TService : class
        {
            throw new NotImplementedException($"To resolve services from {nameof(IConsumeScopeProvider)} register {nameof(DefaultConsumeScopeProvider)} or your custom scope provider.");
        }
    }

    /// <inheritdoc />
    public IServiceResolverScope CreateScope() => Scope;
}
