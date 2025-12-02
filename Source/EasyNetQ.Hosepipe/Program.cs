using System.Reflection;
using System.Text;
using EasyNetQ.Consumer;

namespace EasyNetQ.Hosepipe;

public class Program
{
    private readonly ArgParser argParser;
    private readonly IQueueRetrieval queueRetrieval;
    private readonly IMessageWriter messageWriter;
    private readonly IMessageReader messageReader;
    private readonly IQueueInsertion queueInsertion;
    private readonly IErrorRetry errorRetry;
    private readonly IConventions conventions;

    private static readonly StringBuilder Results = new();
    private static bool succeeded = true;
    private static readonly Func<string, Action> Message = m => () =>
    {
        Results.AppendLine(m);
        succeeded = false;
    };

    public Program(
        ArgParser argParser,
        IQueueRetrieval queueRetrieval,
        IMessageWriter messageWriter,
        IMessageReader messageReader,
        IQueueInsertion queueInsertion,
        IErrorRetry errorRetry,
        IConventions conventions)
    {
        this.argParser = argParser;
        this.queueRetrieval = queueRetrieval;
        this.messageWriter = messageWriter;
        this.messageReader = messageReader;
        this.queueInsertion = queueInsertion;
        this.errorRetry = errorRetry;
        this.conventions = conventions;
    }

    public static async Task Main(string[] args)
    {
        var typeNameSerializer = new LegacyTypeNameSerializer();
        var argParser = new ArgParser();
        var arguments = argParser.Parse(args);

        var enableBinaryPayloads = false;
        arguments.WithTypedKeyOptional<bool>("b", a => enableBinaryPayloads = bool.Parse(a.Value))
            .FailWith(Message("Invalid enable binary payloads (b) parameter"));

        IErrorMessageSerializer errorMessageSerializer;
        if (enableBinaryPayloads)
        {
            errorMessageSerializer = new Base64ErrorMessageSerializer();
        }
        else
        {
            errorMessageSerializer = new DefaultErrorMessageSerializer();
        }

        // poor man's dependency injection FTW ;)
        var program = new Program(
            argParser,
            new QueueRetrieval(errorMessageSerializer),
            new FileMessageWriter(),
            new MessageReader(),
            new QueueInsertion(errorMessageSerializer),
            new ErrorRetry(new ReflectionBasedNewtonsoftJsonSerializer(), errorMessageSerializer),
            new Conventions(typeNameSerializer)
        );

        using var cts = new CancellationTokenSource();
        await program.StartAsync(args, cts.Token);
    }

    public async Task StartAsync(string[] args, CancellationToken cancellationToken)
    {
        var arguments = argParser.Parse(args);

        var parameters = new QueueParameters();
        arguments.WithKey("s", a => parameters.HostName = a.Value);
        arguments.WithKey("sp", a => parameters.HostPort = Convert.ToInt32(a.Value));
        arguments.WithKey("v", a => parameters.VHost = a.Value);
        arguments.WithKey("u", a => parameters.Username = a.Value);
        arguments.WithKey("p", a => parameters.Password = a.Value);
        arguments.WithKey("o", a => parameters.MessagesOutputDirectory = a.Value);
        arguments.WithKey("q", a => parameters.QueueName = a.Value);
        arguments.WithTypedKeyOptional<int>("n", a => parameters.NumberOfMessagesToRetrieve = int.Parse(a.Value))
            .FailWith(Message("Invalid number of messages to retrieve"));
        arguments.WithTypedKeyOptional<bool>("x", a => parameters.Purge = bool.Parse(a.Value))
            .FailWith(Message("Invalid purge (x) parameter"));
        arguments.WithTypedKeyOptional<bool>("ssl", a => parameters.Ssl = bool.Parse(a.Value))
            .FailWith(Message("Invalid Ssl (ssl) parameter"));

        try
        {
            if (arguments.At(0, "dump", () =>
                {
                    arguments.WithKey("q", a =>
                    {
                        parameters.QueueName = a.Value;
                    }).FailWith(Message("No Queue Name given"));
                }) != null)
            {
                await DumpAsync(parameters, cancellationToken);
            }

            arguments.At(0, "insert", async () => await InsertAsync(parameters, cancellationToken));

            arguments.At(0, "err", async () => await ErrorDumpAsync(parameters, cancellationToken));

            arguments.At(0, "retry", async () => await RetryAsync(parameters, cancellationToken));

            arguments.At(0, "?", PrintUsage);

            arguments.At(0, _ => { }).FailWith(PrintUsage);
        }
        catch (EasyNetQHosepipeException easyNetQHosepipeException)
        {
            Console.WriteLine("Operation Failed:");
            Console.WriteLine(easyNetQHosepipeException.Message);
        }

        if (!succeeded)
        {
            Console.WriteLine("Operation failed");
            Console.Write(Results.ToString());
            Console.WriteLine();
            PrintUsage();
        }
    }

    private async Task DumpAsync(QueueParameters parameters, CancellationToken cancellationToken = default)
    {
        var count = 0;
        await messageWriter.WriteAsync(WithEachAsync(queueRetrieval.GetMessagesFromQueueAsync(parameters, cancellationToken), () => count++), parameters, cancellationToken);
    }

    private async Task InsertAsync(QueueParameters parameters, CancellationToken cancellationToken)
    {
        var count = 0;
        await queueInsertion.PublishMessagesToQueueAsync(
            WithEachAsync(messageReader.ReadMessagesAsync(parameters, cancellationToken), () => count++), parameters, cancellationToken
        );

        Console.WriteLine(
            "{0} messages from directory '{1}' were inserted into queue '{2}'",
            count, parameters.MessagesOutputDirectory, parameters.QueueName
        );
    }

    private async Task ErrorDumpAsync(QueueParameters parameters, CancellationToken cancellationToken)
    {
        if (parameters.QueueName == null)
            parameters.QueueName = conventions.ErrorQueueNamingConvention(default);
        await DumpAsync(parameters, cancellationToken);
    }

    private async Task RetryAsync(QueueParameters parameters, CancellationToken cancellationToken)
    {
        var count = 0;
        var queueName = parameters.QueueName ?? conventions.ErrorQueueNamingConvention(default);

        await errorRetry.RetryErrorsAsync(
            WithEachAsync(messageReader.ReadMessagesAsync(parameters, queueName, cancellationToken), () => count++), parameters, cancellationToken
        );

        Console.WriteLine(
            "{0} error messages from directory '{1}' were republished", count, parameters.MessagesOutputDirectory
        );
    }

    private static async IAsyncEnumerable<HosepipeMessage> WithEachAsync(IAsyncEnumerable<HosepipeMessage> messages, Action action)
    {
        await foreach (var message in messages)
        {
            action();
            yield return message;
        }
    }

    private static void PrintUsage()
    {
        using var manifest = typeof(Program).GetTypeInfo().Assembly.GetManifestResourceStream("EasyNetQ.Hosepipe.Usage.txt");
        if (manifest == null)
        {
            throw new Exception("Could not load usage");
        }

        using var reader = new StreamReader(manifest);
        Console.Write(reader.ReadToEnd());
    }
}
