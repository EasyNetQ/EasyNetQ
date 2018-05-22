// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.IO;

using EasyNetQ.Consumer;

using Xunit;

namespace EasyNetQ.Hosepipe.Tests
{
    public class ProgramTests
    {
        private Program program;
        private MockMessageWriter messageWriter;
        private MockQueueRetrieval queueRetrieval;
        private MockMessageReader messageReader;
        private MockQueueInsertion queueInsertion;
        private MockErrorRetry errorRetry;
        private Conventions conventions;
        private IErrorMessageSerializer defaultErrorMessageSerializer;

        public ProgramTests()
        {
            messageWriter = new MockMessageWriter();
            queueRetrieval = new MockQueueRetrieval();
            messageReader = new MockMessageReader();
            queueInsertion = new MockQueueInsertion();
            errorRetry = new MockErrorRetry();
            conventions = new Conventions(new LegacyTypeNameSerializer());
            defaultErrorMessageSerializer = new DefaultErrorMessageSerializer();

            program = new Program(
                new ArgParser(),
                queueRetrieval,
                messageWriter,
                messageReader,
                queueInsertion,
                errorRetry,
                conventions);
        }

        private readonly string expectedDumpOutput =
            "2 Messages from queue 'EasyNetQ_Default_Error_Queue'\r\noutput to directory '" + Directory.GetCurrentDirectory() + "'\r\n";

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

            writer.GetStringBuilder().ToString().ShouldEqual(expectedDumpOutput);

            messageWriter.Parameters.QueueName.ShouldEqual("EasyNetQ_Default_Error_Queue");
            messageWriter.Parameters.HostName.ShouldEqual("localhost");
        }

        private readonly string expectedInsertOutput =
            "2 Messages from directory '" + Directory.GetCurrentDirectory() + "'\r\ninserted into queue ''\r\n";

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

            writer.GetStringBuilder().ToString().ShouldEqual(expectedInsertOutput);

            messageReader.Parameters.HostName.ShouldEqual("localhost");
        }

        private readonly string expectedRetryOutput =
            "2 Error messages from directory '" + Directory.GetCurrentDirectory() + "' republished\r\n";


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
            foreach (var message in messages)
            {
                // Console.Out.WriteLine("message = {0}", message);
            }
        }
    }

    public class MockQueueRetrieval : IQueueRetreival
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
            foreach (var message in messages)
            {
                // Console.Out.WriteLine("message = {0}", message);
            }
        }
    }

    public class MockErrorRetry : IErrorRetry
    {
        public void RetryErrors(IEnumerable<HosepipeMessage> rawErrorMessages, QueueParameters parameters)
        {
            foreach (var rawErrorMessage in rawErrorMessages)
            {
                //
            }
        }
    }
}

// ReSharper restore InconsistentNaming