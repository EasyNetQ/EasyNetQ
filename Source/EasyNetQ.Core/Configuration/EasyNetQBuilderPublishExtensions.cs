using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Configuration;

/// <summary>
///     Fluent publish registration
/// </summary>
public static class EasyNetQBuilderPublishExtensions
{
    /// <summary>
    ///     Registers a transport-agnostic publish definition, used by <see cref="IMessagePublisher" />
    /// </summary>
    public static IEasyNetQBuilder Publish(this IEasyNetQBuilder builder, Action<GenericPublishBuilder> configure)
        => builder.RegisterPublisher(new GenericPublishBuilder(new PublishDefinition()), configure);

    /// <summary>
    ///     Registers a publish definition built by a transport-typed builder. Transports call this from their own
    ///     fluent entry points.
    /// </summary>
    public static IEasyNetQBuilder RegisterPublisher<TBuilder>(this IEasyNetQBuilder builder, TBuilder publishBuilder, Action<TBuilder> configure)
        where TBuilder : PublishBuilder<TBuilder>
    {
        configure(publishBuilder);
        builder.Services.AddSingleton(publishBuilder.Definition);
        return builder;
    }
}
