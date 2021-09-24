using System;

namespace EasyNetQ.DI
{
    /// <summary>
    /// Provides dependency resolution scope for <see cref="RabbitAdvancedBus.Consume(Action{IConsumeConfiguration})"/>
    /// </summary>
    public interface IConsumeScopeProvider
    {
        /// <summary>
        /// Creates scope
        /// </summary>
        IDisposable CreateScope();
    }

    /// <summary>
    /// Default scope provider that uses already registered <see cref="IServiceResolver"/>
    /// </summary>
    public class DefaultConsumeScopeProvider : IConsumeScopeProvider
    {
        private readonly IServiceResolver _resolver;

        /// <summary>
        /// Creates default provider
        /// </summary>
        public DefaultConsumeScopeProvider(IServiceResolver resolver)
        {
            _resolver = resolver;
        }

        /// <inheritdoc />
        public IDisposable CreateScope() => _resolver.CreateScope();
    }

    /// <summary>
    /// Noop scope provider used for backward compatibility especially for <see cref="AutoSubscribe.AutoSubscriber"/>
    /// </summary>
    public class NoopConsumeScopeProvider : IConsumeScopeProvider
    {
        private static readonly IDisposable scope = new NoopDisposable();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }

        /// <inheritdoc />
        public IDisposable CreateScope() => scope;
    }
}
