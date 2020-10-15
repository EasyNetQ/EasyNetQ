using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using EasyNetQ.Consumer;

namespace EasyNetQ.Hosepipe
{
    public class Program
    {
        private readonly ArgParser argParser;
        private readonly IQueueRetrieval queueRetrieval;
        private readonly IMessageWriter messageWriter;
        private readonly IMessageReader messageReader;
        private readonly IQueueInsertion queueInsertion;
        private readonly IErrorRetry errorRetry;
        private readonly IConventions conventions;

        private static readonly StringBuilder Results = new StringBuilder();
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

        public static void Main(string[] args)
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
                new ErrorRetry(new JsonSerializer(), errorMessageSerializer),
                new Conventions(typeNameSerializer)
            );
            program.Start(args);
        }

        public void Start(string[] args)
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

            try
            {
                arguments.At(0, "dump", () => arguments.WithKey("q", a =>
                {
                    parameters.QueueName = a.Value;
                    Dump(parameters);
                }).FailWith(Message("No Queue Name given")));

                arguments.At(0, "insert", () => Insert(parameters));

                arguments.At(0, "err", () => ErrorDump(parameters));

                arguments.At(0, "retry", () => Retry(parameters));

                arguments.At(0, "?", PrintUsage);

                // print usage if there are no arguments
                arguments.At(0, a => { }).FailWith(PrintUsage);
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

        private void Dump(QueueParameters parameters)
        {
            var count = 0;
            messageWriter.Write(WithEach(queueRetrieval.GetMessagesFromQueue(parameters), () => count++), parameters);

            Console.WriteLine(
                "{0} messages from queue '{1}' were dumped to directory '{2}'",
                count, parameters.QueueName, parameters.MessagesOutputDirectory
            );
        }

        private void Insert(QueueParameters parameters)
        {
            var count = 0;
            queueInsertion.PublishMessagesToQueue(
                WithEach(messageReader.ReadMessages(parameters), () => count++), parameters
            );

            Console.WriteLine(
                "{0} messages from directory '{1}' were inserted into queue '{2}'",
                count, parameters.MessagesOutputDirectory, parameters.QueueName
            );
        }

        private void ErrorDump(QueueParameters parameters)
        {
            if (parameters.QueueName == null)
                parameters.QueueName = conventions.ErrorQueueNamingConvention(null);
            Dump(parameters);
        }

        private void Retry(QueueParameters parameters)
        {
            var count = 0;
            var queueName = parameters.QueueName ?? conventions.ErrorQueueNamingConvention(null);

            errorRetry.RetryErrors(
                WithEach(messageReader.ReadMessages(parameters, queueName), () => count++), parameters
            );

            Console.WriteLine(
                "{0} error messages from directory '{1}' were republished", count, parameters.MessagesOutputDirectory
            );
        }

        private static IEnumerable<HosepipeMessage> WithEach(IEnumerable<HosepipeMessage> messages, Action action)
        {
            foreach (var message in messages)
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
}
