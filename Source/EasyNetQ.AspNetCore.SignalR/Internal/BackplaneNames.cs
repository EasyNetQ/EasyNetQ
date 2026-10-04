using System.Security.Cryptography;
using System.Text;

namespace EasyNetQ.AspNetCore.SignalR.Internal;

/// <summary>
///     Exchange, queue and routing-key names. The hub exchange is <b>direct</b>: group, user and connection names are
///     user data and must never act as topic wildcards (<c>*</c>, <c>#</c>). AMQP caps names at 255 bytes, so longer
///     ones are replaced by a SHA-256; a different separator keeps hashed and literal keys from colliding.
/// </summary>
internal sealed class BackplaneNames
{
    private const int MaxLength = 255;

    public BackplaneNames(string prefix, string hubName, string serverName)
    {
        Exchange = Limit($"{prefix}.{hubName}");
        Queue = Limit($"{prefix}.{hubName}.{serverName}");
        OwnAck = AckFor(serverName);
        OwnReturn = ReturnFor(serverName);
    }

    public string Exchange { get; }
    public string Queue { get; }
    public string OwnAck { get; }
    public string OwnReturn { get; }

    public const string All = "all";
    public const string Groups = "groups";

    public static string Connection(string connectionId) => Key("conn", connectionId);
    public static string Group(string groupName) => Key("group", groupName);
    public static string User(string userId) => Key("user", userId);
    public static string AckFor(string serverName) => Key("ack", serverName);
    public static string ReturnFor(string serverName) => Key("return", serverName);

    private static string Key(string kind, string value)
    {
        var key = $"{kind}.{value}";
        return Encoding.UTF8.GetByteCount(key) <= MaxLength ? key : $"{kind}:{Hash(value)}";
    }

    private static string Limit(string name)
        => Encoding.UTF8.GetByteCount(name) <= MaxLength ? name : $"signalr:{Hash(name)}";

    private static string Hash(string value)
    {
#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
#else
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
#endif
    }
}
