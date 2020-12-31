using System;
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
    public sealed class ReadOnlyMemoryStream : Stream
    {
        private readonly ReadOnlyMemory<byte> content;
        private int position;

        /// <inheritdoc />
        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> content)
        {
            this.content = content;
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => content.Length;

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
        public override long Seek(long offset, SeekOrigin origin)
        {
            var newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => position + offset,
                SeekOrigin.End => content.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            if (newPosition > int.MaxValue || newPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            position = (int)newPosition;
            return position;
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            var span = content.Span;
            return position < span.Length ? span[position++] : -1;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadArrayArguments(buffer, offset, count);

            var remaining = content.Length - position;
            if (remaining <= 0 || buffer.Length == 0)
                return 0;

            if (remaining <= buffer.Length)
            {
                content.Span.Slice(position).CopyTo(buffer);
                position = content.Length;
                return remaining;
            }

            content.Span.Slice(position, buffer.Length).CopyTo(buffer);
            position += buffer.Length;
            return buffer.Length;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

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
