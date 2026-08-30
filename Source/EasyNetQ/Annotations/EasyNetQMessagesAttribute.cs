namespace EasyNetQ;

/// <summary>
///     Assembly-level opt-in for the source generator: declares message types that cannot be discovered from call
///     sites or annotations (e.g. types from a contract assembly that this assembly only relays).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class EasyNetQMessagesAttribute : Attribute
{
    /// <summary>
    ///     Declares <paramref name="messageTypes" /> as message types to register.
    /// </summary>
    public EasyNetQMessagesAttribute(params Type[] messageTypes)
    {
        MessageTypes = messageTypes;
    }

    /// <summary>
    ///     The declared message types.
    /// </summary>
    public Type[] MessageTypes { get; }
}
