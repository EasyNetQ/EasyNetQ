using System.Buffers;

namespace EasyNetQ.Internals;

internal sealed class EmptyMemoryOwner : IMemoryOwner<byte>
{
    public static readonly EmptyMemoryOwner Instance = new();

    public void Dispose()
    {
    }

    public Memory<byte> Memory => Memory<byte>.Empty;
}
