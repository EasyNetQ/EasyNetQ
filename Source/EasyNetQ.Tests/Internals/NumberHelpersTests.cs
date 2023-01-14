using System.Text;
using EasyNetQ.Internals;

namespace EasyNetQ.Tests.Internals;

public class NumberHelpersTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(9L)]
    [InlineData(10)]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(999)]
    [InlineData(1000)]
    [InlineData(9999)]
    [InlineData(10000)]
    [InlineData(99999)]
    [InlineData(ulong.MaxValue)]
    public void TestDigitBytesCount(ulong value)
    {
        NumberHelpers.ULongBytesCount(value).Should().Be(value.ToString().Length);
    }


    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(9L)]
    [InlineData(10)]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(999)]
    [InlineData(1000)]
    [InlineData(9999)]
    [InlineData(10000)]
    [InlineData(99999)]
    [InlineData(ulong.MaxValue)]
    public void TestToDigitBytes(ulong value)
    {
        NumberHelpers.FormatULongToBytes(value).Should().BeEquivalentTo(Encoding.UTF8.GetBytes(value.ToString()));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(9L)]
    [InlineData(10)]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(999)]
    [InlineData(1000)]
    [InlineData(9999)]
    [InlineData(10000)]
    [InlineData(99999)]
    [InlineData(ulong.MaxValue)]
    public void TestFromDigitBytes(ulong value)
    {
        var bytes = Encoding.UTF8.GetBytes(value.ToString());
        NumberHelpers.TryParseULongFromBytes(bytes, out var result).Should().BeTrue();
        result.Should().Be(ulong.Parse(Encoding.UTF8.GetString(bytes)));
    }
}
