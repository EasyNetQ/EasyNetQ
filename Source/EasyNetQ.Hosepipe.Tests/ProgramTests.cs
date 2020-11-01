// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace EasyNetQ.Hosepipe.Tests
{
    public class ProgramTests
    {
        private readonly Program program;
        private readonly MockMessageWriter messageWriter;
        private readonly MockQueueRetrieval queueRetrieval;
        private readonly MockMessageReader messageReader;
        private readonly MockQueueInsertion queueInsertion;
        private readonly MockErrorRetry errorRetry;
        private readonly Conventions conventions;

        public ProgramTests()
        {
            messageWriter = new MockMessageWriter();
            queueRetrieval = new MockQueueRetrieval();
            messageReader = new MockMessageReader();
            queueInsertion = new MockQueueInsertion();
            errorRetry = new MockErrorRetry();
            conventions = new Conventions(new LegacyTypeNameSerializer());

            program = new Program(
                new ArgParser(),
                queueRetrieval,
                messageWriter,
                messageReader,
                queueInsertion,
                errorRetry,
                conventions
            );
        }

        private readonly string expectedDumpOutput =
            $"2 messages from queue 'EasyNetQ_Default_Error_Queue' were dumped to directory '{Directory.GetCurrentDirectory()}'{Environment.NewLine}";

        [Fact]
        public void Should_output_messages_to_directory_with_dump()
        {
            var args = new[]
            {
                "dump",
                "s:localhost",
                "q:EasyNetQ_Default_Error_Queue"
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            program.Start(args);

            var actualOutput = writer.GetStringBuilder().ToString();
            actualOutput.ShouldEqual(expectedDumpOutput);

            messageWriter.Parameters.QueueName.ShouldEqual("EasyNetQ_Default_Error_Queue");
            messageWriter.Parameters.HostName.ShouldEqual("localhost");
        }

        private readonly string expectedInsertOutput =
            $"{2} messages from directory '{Directory.GetCurrentDirectory()}' were inserted into queue ''{Environment.NewLine}";

        [Fact]
        public void Should_insert_messages_with_insert()
        {
            var args = new[]
            {
                "insert",
                "s:localhost"
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            program.Start(args);

            var actualInsertOutput = writer.GetStringBuilder().ToString();
            actualInsertOutput.ShouldEqual(expectedInsertOutput);

            messageReader.Parameters.HostName.ShouldEqual("localhost");
        }

        private readonly string expectedInsertOutputWithQueue =
            $"{2} messages from directory '{Directory.GetCurrentDirectory()}' were inserted into queue 'queue'{Environment.NewLine}";

        [Fact]
        public void Should_insert_messages_with_insert_and_queue()
        {
            var args = new[]
            {
                "insert",
                "s:localhost",
                "q:queue"
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            program.Start(args);

            var actualInsertOutput = writer.GetStringBuilder().ToString();
            actualInsertOutput.ShouldEqual(expectedInsertOutputWithQueue);

            messageReader.Parameters.HostName.ShouldEqual("localhost");
        }

        private readonly string expectedRetryOutput =
            $"2 error messages from directory '{Directory.GetCurrentDirectory()}' were republished{Environment.NewLine}";

        [Fact]
        public void Should_retry_errors_with_retry()
        {
            var args = new[]
            {
                "retry",
                "s:localhost"
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            program.Start(args);

            writer.GetStringBuilder().ToString().ShouldEqual(expectedRetryOutput);

            messageReader.Parameters.HostName.ShouldEqual("localhost");
        }
    }

    public class MockMessageWriter : IMessageWriter
    {
        public QueueParameters Parameters { get; set; }

        public void Write(IEnumerable<HosepipeMessage> messages, QueueParameters queueParameters)
        {
            Parameters = queueParameters;
            foreach (var _ in messages)
            {
            }
        }
    }

    public class MockQueueRetrieval : IQueueRetrieval
    {
        public IEnumerable<HosepipeMessage> GetMessagesFromQueue(QueueParameters parameters)
        {
            yield return new HosepipeMessage("some message", new MessageProperties(), Helper.CreateMessageReceivedInfo());
            yield return new HosepipeMessage("some message", new MessageProperties(), Helper.CreateMessageReceivedInfo());
        }
    }

    public class MockMessageReader : IMessageReader
    {
        public QueueParameters Parameters { get; set; }

        public IEnumerable<HosepipeMessage> ReadMessages(QueueParameters parameters)
        {
            Parameters = parameters;
            yield return new HosepipeMessage("some message", new MessageProperties(), Helper.CreateMessageReceivedInfo());
            yield return new HosepipeMessage("some message", new MessageProperties(), Helper.CreateMessageReceivedInfo());
        }

        public IEnumerable<HosepipeMessage> ReadMessages(QueueParameters parameters, string messageName)
        {
            return ReadMessages(parameters);
        }
    }

    public class MockQueueInsertion : IQueueInsertion
    {
        public void PublishMessagesToQueue(IEnumerable<HosepipeMessage> messages, QueueParameters parameters)
        {
            foreach (var _ in messages)
            {
            }
        }
    }

    public class MockErrorRetry : IErrorRetry
    {
        public void RetryErrors(IEnumerable<HosepipeMessage> rawErrorMessages, QueueParameters parameters)
        {
            foreach (var _ in rawErrorMessages)
            {
            }
        }
    }
}

// ReSharper restore InconsistentNaming
