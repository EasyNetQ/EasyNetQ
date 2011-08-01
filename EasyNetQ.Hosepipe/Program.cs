using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace EasyNetQ.Hosepipe
{
    public class Program
    {
        private readonly ArgParser argParser;
        private readonly IQueueRetreival queueRetreival;
        private readonly IMessageWriter messageWriter;
        private readonly IMessageReader messageReader;
        private readonly IQueueInsertion queueInsertion;


        public Program(
            ArgParser argParser, 
            IQueueRetreival queueRetreival, 
            IMessageWriter messageWriter, 
            IMessageReader messageReader, 
            IQueueInsertion queueInsertion)
        {
            this.argParser = argParser;
            this.queueRetreival = queueRetreival;
            this.messageWriter = messageWriter;
            this.messageReader = messageReader;
            this.queueInsertion = queueInsertion;
        }

        public static void Main(string[] args)
        {
            // poor man's dependency injection FTW ;)
            var program = new Program(
                new ArgParser(), 
                new QueueRetreival(), 
                new FileMessageWriter(),
                new MessageReader(), 
                new QueueInsertion());
            program.Start(args);
        }

        public void Start(string[] args)
        {
            var arguments = argParser.Parse(args);

            var results = new StringBuilder();
            var succeeded = true;
            Func<string, Action> messsage = m => () =>
            {
                results.AppendLine(m);
                succeeded = false;
            };

            var parameters = new QueueParameters();
            arguments.WithKey("s", a => parameters.HostName = a.Value);
            arguments.WithKey("v", a => parameters.VHost = a.Value);
            arguments.WithKey("u", a => parameters.Username = a.Value);
            arguments.WithKey("p", a => parameters.Password = a.Value);
            
            arguments.WithKey("o", a => parameters.MessageFilePath = a.Value);

            arguments.At(0, "dump", () =>
            {
                arguments.WithKey("q", a => parameters.QueueName = a.Value).FailWith(messsage("No Queue Name given"));
                Dump(parameters);
            });
            arguments.At(0, "insert", () =>
            {
                arguments.WithKey("q", a => parameters.QueueName = a.Value).FailWith(messsage("No Queue Name given"));
                Insert(parameters);
            });
            arguments.At(0, "?", PrintUsage);

            if(!succeeded)
            {
                Console.WriteLine("Operation failed");
                Console.Write(results.ToString());
                Console.WriteLine();
                PrintUsage();
            }
        }

        private void Dump(QueueParameters parameters)
        {
            var count = 0;
            messageWriter.Write(WithEach(queueRetreival.GetMessagesFromQueue(parameters), () => count++), parameters);
            Console.WriteLine("{0} Messages from queue '{1}'\r\noutput to directory '{2}'", 
                count, parameters.QueueName, parameters.MessageFilePath);
        }

        private void Insert(QueueParameters parameters)
        {
            var count = 0;
            queueInsertion.PublishMessagesToQueue(
                WithEach(messageReader.ReadMessages(parameters), () => count++), parameters);       
            Console.WriteLine("{0} Messages from directory '{1}'\r\ninserted into queue '{2}'",
                count, parameters.MessageFilePath, parameters.QueueName);
        }

        private IEnumerable<string> WithEach(IEnumerable<string> messages, Action action)
        {
            foreach (var message in messages)
            {
                action();
                yield return message;
            }
        }

        public static void PrintUsage()
        {
            using (var manifest = Assembly.GetExecutingAssembly().GetManifestResourceStream("EasyNetQ.Hosepipe.Usage.txt"))
            {
                if(manifest == null)
                {
                    throw new Exception("Could not load usage");
                }
                using (var reader = new StreamReader(manifest))
                {
                    Console.Write(reader.ReadToEnd());
                }
            }
        }
    }
}
