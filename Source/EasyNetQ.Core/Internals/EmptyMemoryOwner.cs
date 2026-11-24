using System.Buffers;

namespace EasyNetQ.Internals;

/// <summary>
///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
///     the same compatibility as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new EasyNetQ release.
/// </summary>
public sealed class EmptyMemoryOwner : IMemoryOwner<byte>
{
    public static readonly EmptyMemoryOwner Instance = new();

    public void Dispose()
    {
    }

    public Memory<byte> Memory => Memory<byte>.Empty;
}
