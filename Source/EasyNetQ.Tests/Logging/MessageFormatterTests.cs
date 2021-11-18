using System;
using System.Globalization;
using EasyNetQ.Logging;
using Shouldly;
using Xunit;

namespace EasyNetQ.Tests.Logging
{
    public class MessageFormatterTests
    {
        [Fact]
        public void When_arguments_are_unique_and_not_escaped_Then_should_replace_them()
        {
            string MessageBuilder() => "This is an {1argument} and this another {argument2} and a last one {2}.";

            var formattedMessage = MessageFormatter.SimulateStructuredLogging(MessageBuilder, new object[] { "arg0", "arg1", "arg2" })();

            formattedMessage.ShouldBe("This is an arg0 and this another arg1 and a last one arg2.");
        }

        [Fact]
        public void When_arguments_are_escaped_Then_should_not_replace_them()
        {
            string MessageBuilder() => "This is an {argument} and this an {{escaped_argument}}.";

            var formattedMessage = MessageFormatter.SimulateStructuredLogging(MessageBuilder, new object[] { "arg0", "arg1" })();

            formattedMessage.ShouldBe("This is an arg0 and this an {escaped_argument}.");
        }

        [Fact]
        public void When_argument_has_format_Then_should_preserve_format()
        {
            var date = DateTime.Today;
            string MessageBuilder() => "Formatted {date1:yyyy-MM-dd} and not formatted {date2}.";

            var formattedMessage = MessageFormatter.SimulateStructuredLogging(MessageBuilder, new object[] { date, date })();

            formattedMessage.ShouldBe(
                string.Format(CultureInfo.InvariantCulture, "Formatted {0:yyyy-MM-dd} and not formatted {1}.", date, date));
        }

        [Fact]
        public void When_argument_is_multiple_time_Then_should_be_replaced_with_same_value()
        {
            var date = DateTime.Today;
            string MessageBuilder() => "{date:yyyy-MM-dd} {argument1} {date:yyyy}";

            var formattedMessage = MessageFormatter.SimulateStructuredLogging(MessageBuilder, new object[] { date, "arg0" })();

            formattedMessage.ShouldBe(
                string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd} {1} {0:yyyy}", date, "arg0")
            );
        }
    }
}
