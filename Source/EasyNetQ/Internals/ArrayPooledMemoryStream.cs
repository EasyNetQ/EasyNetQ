using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public sealed class ArrayPooledMemoryStream : Stream, IMemoryOwner<byte>
    {
        private byte[] rentBuffer;
        private int length;
        private int position;

        /// <inheritdoc />
        public ArrayPooledMemoryStream()
        {
            rentBuffer = Array.Empty<byte>();
            length = 0;
            position = 0;
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => length;

        /// <inheritdoc />
        public override long Position
        {
            get => position;
            set
            {
                if (value < 0 || value > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value));

                position = (int)value;
            }
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <summary>set stream length</summary>
        /// <remarks>if length is larger than current buffer length, re-allocating buffer</remarks>
        /// <exception cref="System.InvalidOperationException">if stream is readonly</exception>
        public override void SetLength(long value)
        {
            if (value > int.MaxValue)
                throw new IndexOutOfRangeException("overflow");

            if (value < 0)
                throw new IndexOutOfRangeException("underflow");

            length = (int)value;
            if (rentBuffer.Length < length)
                ReallocateBuffer(length);

            if (position < length)
                return;

            position = length == 0 ? 0 : length - 1;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            var newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => position + offset,
                SeekOrigin.End => rentBuffer.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            if (newPosition > int.MaxValue || newPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            position = (int)newPosition;
            return position;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadArrayArguments(buffer, offset, count);

            var byteRead = count > length - position ? length - position : count;
            if (byteRead == 0)
                return 0;

            Buffer.BlockCopy(rentBuffer, position, buffer, offset, byteRead);
            position += byteRead;
            return byteRead;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            var endOffset = position + count;
            if (endOffset > rentBuffer.Length)
                ReallocateBuffer(endOffset * 2);
            Buffer.BlockCopy(buffer, offset, rentBuffer, position, count);
            if (endOffset > length)
                length = endOffset;
            position = endOffset;
        }

        /// <inheritdoc />
        public Memory<byte> Memory => rentBuffer.AsMemory(0, length);

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (rentBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentBuffer);
                rentBuffer = null;
            }

            length = 0;
            position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReallocateBuffer(int minimumRequired)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(minimumRequired);
            Buffer.BlockCopy(rentBuffer, 0, newBuffer, 0, rentBuffer.Length < newBuffer.Length ? rentBuffer.Length : newBuffer.Length);
            ArrayPool<byte>.Shared.Return(rentBuffer);
            rentBuffer = newBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateReadArrayArguments(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0 || buffer.Length - offset < count)
                throw new ArgumentOutOfRangeException(nameof(count));
        }
    }
}
