// ReSharper disable InconsistentNaming

using NUnit.Framework;
using EasyNetQ.AmqpExceptions;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class AmqpExceptionParserTests
    {
        [Test]
        public void Should_parse_first_Amqp_exception_example()
        {
            const string originalException =
                "The AMQP operation was interrupted: AMQP close-reason, initiated by Peer, code=320, " +
                "text=\"CONNECTION_FORCED - Closed via management plugin\", classId=0, methodId=0, cause=";

            var amqpException = AmqpExceptionGrammar.ParseExceptionString(originalException);

            amqpException.Preface.Text.ShouldEqual("The AMQP operation was interrupted");
            amqpException.Code.ShouldEqual(320);
            amqpException.MethodId.ShouldEqual(0);
            amqpException.ClassId.ShouldEqual(0);
        }

        [Test]
        public void Should_parse_second_Amqp_exception_example()
        {
            const string originalException =
                "The AMQP operation was interrupted: AMQP close-reason, initiated by Peer, code=406, " +
                "text=\"PRECONDITION_FAILED - cannot redeclare exchange 'myExchange' in vhost '/' " +
                "with different type, durable, internal or autodelete value\", classId=40, methodId=10, cause=";

            var amqpException = AmqpExceptionGrammar.ParseExceptionString(originalException);

            amqpException.Preface.Text.ShouldEqual("The AMQP operation was interrupted");
            amqpException.Code.ShouldEqual(406);
            amqpException.MethodId.ShouldEqual(10);
            amqpException.ClassId.ShouldEqual(40);
        }

        [Test]
        [ExpectedException(typeof(Sprache.ParseException))]
        public void Should_fail_on_badly_formatted_exception()
        {
            const string originalException = "Do be do od be do do = something else, that I don't know=hello";

            AmqpExceptionGrammar.ParseExceptionString(originalException);
        }
    }
}

// ReSharper restore InconsistentNaming