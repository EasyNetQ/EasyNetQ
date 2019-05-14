// ReSharper disable InconsistentNaming

using EasyNetQ.AmqpExceptions;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests
{
    public class AmqpExceptionParserTests
    {
        [Fact]
        public void Should_fail_on_badly_formatted_exception()
        {
            Assert.Throws<Sprache.ParseException>(() =>
            {
                const string originalException = "Do be do od be do do = something else, that I don't know=hello";

                AmqpExceptionGrammar.ParseExceptionString(originalException);
            });
        }

        [Fact]
        public void Should_parse_first_Amqp_exception_example()
        {
            const string originalException =
                "The AMQP operation was interrupted: AMQP close-reason, initiated by Peer, code=320, " +
                "text=\"CONNECTION_FORCED - Closed via management plugin\", classId=0, methodId=0, cause=";

            var amqpException = AmqpExceptionGrammar.ParseExceptionString(originalException);

            amqpException.Preface.Text.Should().Be("The AMQP operation was interrupted");
            amqpException.Code.Should().Be(320);
            amqpException.MethodId.Should().Be(0);
            amqpException.ClassId.Should().Be(0);
        }

        [Fact]
        public void Should_parse_second_Amqp_exception_example()
        {
            const string originalException =
                "The AMQP operation was interrupted: AMQP close-reason, initiated by Peer, code=406, " +
                "text=\"PRECONDITION_FAILED - cannot redeclare exchange 'myExchange' in vhost '/' " +
                "with different type, durable, internal or autodelete value\", classId=40, methodId=10, cause=";

            var amqpException = AmqpExceptionGrammar.ParseExceptionString(originalException);

            amqpException.Preface.Text.Should().Be("The AMQP operation was interrupted");
            amqpException.Code.Should().Be(406);
            amqpException.MethodId.Should().Be(10);
            amqpException.ClassId.Should().Be(40);
        }
    }
}

// ReSharper restore InconsistentNaming
