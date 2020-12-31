using System;
using System.IO;
using System.Linq;
using EasyNetQ.Internals;
using Xunit;

namespace EasyNetQ.Tests.Internals
{
    public class ArrayPooledMemoryStreamTests
    {
        [Fact]
        public void Should_write_bytes()
        {
            var data = new byte[] { 1, 2, 3, 4 };
            using var stream = new ArrayPooledMemoryStream();

            Assert.True(stream.CanWrite);
            Assert.True(stream.CanRead);
            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);
            stream.Write(data, 0, data.Length);
            Assert.Equal(4, stream.Length);
            Assert.Equal(4, stream.Position);

            var memory = stream.Memory;
            Assert.Equal(data, memory.ToArray());
            stream.Write(data, 0, data.Length);
            Assert.Equal(8, stream.Length);
            Assert.Equal(8, stream.Position);

            memory = stream.Memory;
            Assert.Equal(data.Concat(data), memory.ToArray());
        }

        [Fact]
        public void Should_write_span()
        {
            var data = new byte[] { 1, 2, 3, 4 }.AsSpan();
            using var stream = new ArrayPooledMemoryStream();

            Assert.True(stream.CanWrite);
            Assert.True(stream.CanRead);
            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);
            stream.Write(data);
            Assert.Equal(4, stream.Length);
            Assert.Equal(4, stream.Position);
            var memory = stream.Memory;
            Assert.Equal(data.ToArray(), memory.ToArray());
            stream.Write(data);
            Assert.Equal(8, stream.Length);
            Assert.Equal(8, stream.Position);
            memory = stream.Memory;
            Assert.Equal(data.ToArray().Concat(data.ToArray()), memory.ToArray());
        }

        [Fact]
        public void Should_write_bytes_to_middle()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using var stream = new ArrayPooledMemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Seek(4, SeekOrigin.Begin);
            stream.Write(data, 0, data.Length);
            var memory = stream.Memory;
            Assert.Equal(data.Take(4).Concat(data), memory.ToArray());
        }

        [Fact]
        public void Should_write_span_to_middle()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using var stream = new ArrayPooledMemoryStream();
            stream.Write(data.AsSpan());
            stream.Seek(4, SeekOrigin.Begin);
            stream.Write(data.AsSpan());
            var memory = stream.Memory;
            Assert.Equal(data.Take(4).Concat(data), memory.ToArray());
        }

        [Fact]
        public void Should_set_length()
        {
            var data = new byte[] { 1, 2, 3, 4 };
            using var stream = new ArrayPooledMemoryStream();
            stream.Write(data, 0, data.Length);
            stream.SetLength(128 * 1024);
            Assert.Equal(128 * 1024, stream.Length);
            var memory = stream.Memory;
            Assert.Equal(data, memory.Span[..4].ToArray());
        }
    }
}
