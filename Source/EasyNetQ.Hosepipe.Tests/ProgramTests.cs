// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

namespace EasyNetQ.Hosepipe.Tests;

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
        conventions = new Conventions(new LegacyTypeNameSerializer(), new MessageTypeRegistry(new LegacyTypeNameSerializer()));

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

    private readonly string expectedInsertOutput =
        $"{2} messages from directory '{Directory.GetCurrentDirectory()}' were inserted into queue ''{Environment.NewLine}";

    [Fact]
    public async Task Should_insert_messages_with_insert()
    {
        var args = new[]
        {
            "insert",
            "s:localhost"
        };

        using var writer = new StringWriter();
        Console.SetOut(writer);

        await program.StartAsync(args, CancellationToken.None);

        var actualInsertOutput = writer.GetStringBuilder().ToString();
        actualInsertOutput.ShouldEqual(expectedInsertOutput);

        messageReader.Parameters.HostName.ShouldEqual("localhost");
    }

    private readonly string expectedInsertOutputWithQueue =
        $"{2} messages from directory '{Directory.GetCurrentDirectory()}' were inserted into queue 'queue'{Environment.NewLine}";

    [Fact]
    public async Task Should_insert_messages_with_insert_and_queue()
    {
        var args = new[]
        {
            "insert",
            "s:localhost",
            "q:queue"
        };

        using var writer = new StringWriter();
        Console.SetOut(writer);

        await program.StartAsync(args, CancellationToken.None);

        var actualInsertOutput = writer.GetStringBuilder().ToString();
        actualInsertOutput.ShouldEqual(expectedInsertOutputWithQueue);

        messageReader.Parameters.HostName.ShouldEqual("localhost");
    }

    private readonly string expectedRetryOutput =
        $"2 error messages from directory '{Directory.GetCurrentDirectory()}' were republished{Environment.NewLine}";

    [Fact]
    public async Task Should_retry_errors_with_retry()
    {
        var args = new[]
        {
            "retry",
            "s:localhost"
        };

        using var writer = new StringWriter();
        Console.SetOut(writer);

        await program.StartAsync(args, CancellationToken.None);

        writer.GetStringBuilder().ToString().ShouldEqual(expectedRetryOutput);

        messageReader.Parameters.HostName.ShouldEqual("localhost");
    }

    [Fact]
    public async Task Should_finish_an_asynchronous_retry_before_returning()
    {
        var slowRetry = new SlowErrorRetry();
        var slowRetrieval = new RecordingQueueRetrieval();
        var sut = new Program(new ArgParser(), slowRetrieval, messageWriter, messageReader, queueInsertion, slowRetry, conventions);

        using var writer = new StringWriter();
        Console.SetOut(writer);

        await sut.StartAsync(new[] { "retry", "s:localhost", "q:app.errors" }, CancellationToken.None);

        slowRetry.Retried.ShouldEqual(2);
        slowRetrieval.Calls.ShouldEqual(0);
    }

    [Fact]
    public async Task Should_finish_an_asynchronous_insert_without_dumping_the_queue_first()
    {
        var slowInsertion = new SlowQueueInsertion();
        var slowRetrieval = new RecordingQueueRetrieval();
        var sut = new Program(new ArgParser(), slowRetrieval, messageWriter, messageReader, slowInsertion, errorRetry, conventions);

        using var writer = new StringWriter();
        Console.SetOut(writer);

        await sut.StartAsync(new[] { "insert", "s:localhost", "q:queue" }, CancellationToken.None);

        slowInsertion.Inserted.ShouldEqual(2);
        slowRetrieval.Calls.ShouldEqual(0);
    }

    [Fact]
    public async Task Should_dump_the_default_error_queue_when_no_queue_is_given()
    {
        using var writer = new StringWriter();
        Console.SetOut(writer);

        await program.StartAsync(new[] { "err", "s:localhost" }, CancellationToken.None);

        messageWriter.Parameters.QueueName.ShouldEqual(conventions.ErrorQueueNamingConvention(default));
    }
}

public class MockMessageWriter : IMessageWriter
{
    public QueueParameters Parameters { get; set; }

    public async Task WriteAsync(
        IAsyncEnumerable<HosepipeMessage> messages,
        QueueParameters queueParameters,
        CancellationToken cancellationToken = default)
    {
        Parameters = queueParameters;
        await foreach (var _ in messages)
        {
        }
    }
}

public class MockQueueRetrieval : IQueueRetrieval
{
    public async IAsyncEnumerable<HosepipeMessage> GetMessagesFromQueueAsync(
        QueueParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new HosepipeMessage("some message", MessageProperties.Empty, Helper.CreateMessageReceivedInfo());
        yield return new HosepipeMessage("some message", MessageProperties.Empty, Helper.CreateMessageReceivedInfo());
        await Task.Yield();
    }
}

public class MockMessageReader : IMessageReader
{
    public QueueParameters Parameters { get; set; }

    public async IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Parameters = parameters;

        yield return new HosepipeMessage("some message", MessageProperties.Empty, Helper.CreateMessageReceivedInfo());
        yield return new HosepipeMessage("some message", MessageProperties.Empty, Helper.CreateMessageReceivedInfo());
        await Task.Yield();
    }

    public IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters, string messageName, CancellationToken cancellationToken = default)
    {
        return ReadMessagesAsync(parameters, cancellationToken);
    }
}

public class MockQueueInsertion : IQueueInsertion
{
    public async Task PublishMessagesToQueueAsync(
        IAsyncEnumerable<HosepipeMessage> messages,
        QueueParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await foreach (var _ in messages)
        {
        }
    }
}

public class MockErrorRetry : IErrorRetry
{
    public async Task RetryErrorsAsync(
        IAsyncEnumerable<HosepipeMessage> rawErrorMessages,
        QueueParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await foreach (var _ in rawErrorMessages)
        {
        }
    }
}

public class RecordingQueueRetrieval : IQueueRetrieval
{
    public int Calls { get; private set; }

    public async IAsyncEnumerable<HosepipeMessage> GetMessagesFromQueueAsync(
        QueueParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls++;
        await Task.Delay(10, cancellationToken);
        yield break;
    }
}

public class SlowQueueInsertion : IQueueInsertion
{
    public int Inserted { get; private set; }

    public async Task PublishMessagesToQueueAsync(
        IAsyncEnumerable<HosepipeMessage> messages,
        QueueParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await foreach (var _ in messages)
        {
            await Task.Delay(20, cancellationToken);
            Inserted++;
        }
    }
}

public class SlowErrorRetry : IErrorRetry
{
    public int Retried { get; private set; }

    public async Task RetryErrorsAsync(
        IAsyncEnumerable<HosepipeMessage> rawErrorMessages,
        QueueParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await foreach (var _ in rawErrorMessages)
        {
            await Task.Delay(20, cancellationToken);
            Retried++;
        }
    }
}

// ReSharper restore InconsistentNaming
