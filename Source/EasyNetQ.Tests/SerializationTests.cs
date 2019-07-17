using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests
{
    public class SerializationTests
    {
        [Fact]
        public void Should_Deserialize_Exceptions()
        {
            var str1 = Newtonsoft.Json.JsonConvert.SerializeObject(new EasyNetQException("blablabla"));
            var ex1 = Newtonsoft.Json.JsonConvert.DeserializeObject<EasyNetQException>(str1);
            ex1.Message.Should().Be("blablabla");

            var str2 = Newtonsoft.Json.JsonConvert.SerializeObject(new EasyNetQResponderException("Ooops"));
            var ex2 = Newtonsoft.Json.JsonConvert.DeserializeObject<EasyNetQResponderException>(str2);
            ex2.Message.Should().Be("Ooops");
        }
    }
}
