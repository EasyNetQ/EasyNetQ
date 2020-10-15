using Xunit;

namespace EasyNetQ.Hosepipe.Tests
{
    public static class TestExtensions
    {
        public static T ShouldEqual<T>(this T actual, object expected)
        {
            Assert.Equal(expected, actual);
            return actual;
        }

        public static void ShouldBeNull(this object actual)
        {
            Assert.Null(actual);
        }

        public static void ShouldBeTrue(this bool source)
        {
            Assert.True(source);
        }
    }
}
