using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace EasyNetQ.Serialization.NewtonsoftJson;

/// <summary>
///     Serializer based on Newtonsoft.Json
/// </summary>
public sealed class NewtonsoftJsonSerializer : ISerializer
{
    private static readonly Newtonsoft.Json.JsonSerializerSettings DefaultSerializerSettings =
        new()
        {
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
        };

    private readonly Newtonsoft.Json.JsonSerializer jsonSerializer;

    /// <inheritdoc />
    public NewtonsoftJsonSerializer() : this(DefaultSerializerSettings)
    {
    }

    /// <summary>
    ///     Creates JsonSerializer
    /// </summary>
    public NewtonsoftJsonSerializer(Newtonsoft.Json.JsonSerializerSettings settings)
    {
        jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(settings);
    }

    /// <inheritdoc />
    public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
    {
        var writer = new Utf8NoBomBytesWriter();

        using var jsonWriter = new Newtonsoft.Json.JsonTextWriter(writer)
        {
            Formatting = jsonSerializer.Formatting,
            ArrayPool = JsonSerializerArrayPool<char>.Instance,
            CloseOutput = false
        };

        jsonSerializer.Serialize(jsonWriter, message, messageType);

        return writer;
    }

    /// <inheritdoc />
    public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
    {
        using var bufferReader = new Utf8NoBomBytesReader(bytes);
        using var reader = new Newtonsoft.Json.JsonTextReader(bufferReader) { ArrayPool = JsonSerializerArrayPool<char>.Instance };
        return jsonSerializer.Deserialize(reader, messageType)!;
    }

    private class JsonSerializerArrayPool<T> : Newtonsoft.Json.IArrayPool<T>
    {
        public static JsonSerializerArrayPool<T> Instance { get; } = new();

        public T[] Rent(int minimumLength) => ArrayPool<T>.Shared.Rent(minimumLength);

        public void Return(T[]? array)
        {
            if (array == null) return;

            ArrayPool<T>.Shared.Return(array);
        }
    }

    private sealed class Utf8NoBomBytesReader : TextReader
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private char[] chars;
        private int length;
        private int position;

        public Utf8NoBomBytesReader(in ReadOnlyMemory<byte> buffer)
        {
            var charsCount = Utf8NoBom.GetMaxCharCount(buffer.Length);
            chars = ArrayPool<char>.Shared.Rent(charsCount);

#if NET6_0_OR_GREATER
            length = Utf8NoBom.GetChars(buffer.Span, chars.AsSpan());
#else
            if (MemoryMarshal.TryGetArray(buffer, out var segment))
            {
                length = segment.Array == null
                    ? 0
                    : Utf8NoBom.GetChars(segment.Array, segment.Offset, segment.Count, chars, 0);
            }
            else
            {
                var bufferArray = ArrayPool<byte>.Shared.Rent(buffer.Length);
                try
                {
                    buffer.CopyTo(bufferArray);

                    length = Utf8NoBom.GetChars(bufferArray, 0, buffer.Length, chars, 0);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferArray);
                }
            }
#endif
        }

        public override int Peek() => position < length ? chars[position] : -1;

        public override int Read()
        {
            if (position == length) return -1;

            return chars[position++];
        }

        protected override void Dispose(bool disposing)
        {
            if (chars == Array.Empty<char>()) return;

            ArrayPool<char>.Shared.Return(chars);
            chars = Array.Empty<char>();
            position = 0;
            length = 0;
        }
    }

    private sealed class Utf8NoBomBytesWriter : TextWriter, IMemoryOwner<byte>
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private int position;
        private byte[] bytesBuffer = Array.Empty<byte>();

        public override Encoding Encoding => Utf8NoBom;

        public override void Write(char value)
        {
            var byteCount = Utf8NoBom.GetMaxByteCount(1);
            var endPosition = position + byteCount;
            if (endPosition > bytesBuffer.Length)
                ReallocateBuffer(endPosition * 2);

            unsafe
            {
                var charsPtr = stackalloc char[1];
                charsPtr[0] = value;

                fixed (byte* bytesBufferPtr = &bytesBuffer[position])
                {
                    position += Utf8NoBom.GetBytes(charsPtr, 1, bytesBufferPtr, byteCount);
                }
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            var endOffset = position + Utf8NoBom.GetMaxByteCount(count);
            if (endOffset > bytesBuffer.Length)
                ReallocateBuffer(endOffset * 2);

            position += Utf8NoBom.GetBytes(buffer, index, count, bytesBuffer, position);
        }

        public Memory<byte> Memory => bytesBuffer.AsMemory(0, position);

        protected override void Dispose(bool disposing)
        {
            if (bytesBuffer == Array.Empty<byte>()) return;

            ArrayPool<byte>.Shared.Return(bytesBuffer);
            bytesBuffer = Array.Empty<byte>();
            position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReallocateBuffer(int minimumRequired)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(minimumRequired);
            Buffer.BlockCopy(bytesBuffer, 0, newBuffer, 0, bytesBuffer.Length < newBuffer.Length ? bytesBuffer.Length : newBuffer.Length);
            ArrayPool<byte>.Shared.Return(bytesBuffer);
            bytesBuffer = newBuffer;
        }
    }
}
