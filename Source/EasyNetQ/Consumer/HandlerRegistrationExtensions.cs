using EasyNetQ.Internals;

namespace EasyNetQ.Consumer;

/// <summary>
///     Version extensions for IHandlerRegistration
/// </summary>
public static class HandlerRegistrationExtensions
{
    /// <summary>
    /// Add an asynchronous handler
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="handlerRegistration">The handler registration</param>
    /// <param name="handler">The handler</param>
    /// <returns></returns>
    public static IHandlerRegistration Add<T>(
        this IHandlerRegistration handlerRegistration, Action<IMessage<T>, MessageReceivedInfo> handler
    )
    {
        var asyncHandler = TaskHelpers.FromAction<IMessage<T>, MessageReceivedInfo>((m, i, _) => handler(m, i));
        return handlerRegistration.Add(asyncHandler);
    }

    /// <summary>
    /// Add an asynchronous handler
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="handlerRegistration">The handler registration</param>
    /// <param name="handler">The handler</param>
    /// <returns></returns>
    public static IHandlerRegistration Add<T>(
        this IHandlerRegistration handlerRegistration, Func<IMessage<T>, MessageReceivedInfo, Task> handler
    ) => handlerRegistration.Add<T>((m, i, _) => handler(m, i));

    /// <summary>
    /// Add an asynchronous handler
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="handlerRegistration">The handler registration</param>
    /// <param name="handler">The handler</param>
    /// <returns></returns>
    public static IHandlerRegistration Add<T>(
        this IHandlerRegistration handlerRegistration,
        Func<IMessage<T>, MessageReceivedInfo, Task<AckStrategy>> handler
    ) => handlerRegistration.Add<T>((m, i, _) => handler(m, i));

    /// <summary>
    /// Add an asynchronous handler
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="handlerRegistration">The handler registration</param>
    /// <param name="handler">The handler</param>
    /// <returns></returns>
    public static IHandlerRegistration Add<T>(
        this IHandlerRegistration handlerRegistration,
        Func<IMessage<T>, MessageReceivedInfo, CancellationToken, Task> handler
    )
    {
        return handlerRegistration.Add<T>(async (m, i, c) =>
        {
            await handler(m, i, c).ConfigureAwait(false);
            return AckStrategies.Ack;
        });
    }
}
