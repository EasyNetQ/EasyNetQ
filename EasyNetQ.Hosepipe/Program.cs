using System;
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

        public Program(ArgParser argParser, IQueueRetreival queueRetreival, IMessageWriter messageWriter)
        {
            this.argParser = argParser;
            this.queueRetreival = queueRetreival;
            this.messageWriter = messageWriter;
        }

        public static void Main(string[] args)
        {
            var program = new Program(
                new ArgParser(), new QueueRetreival(), new FileMessageWriter(@"C:\temp\MessageOutput"));
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
            arguments.WithKey("q", a => parameters.QueueName = a.Value).FailWith(messsage("No Queue Name given"));

            arguments.At(0, "dump", () => Dump(parameters)).FailWith(messsage("No command given"));

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
            messageWriter.Write(queueRetreival.GetMessagesFromQueue(parameters), parameters.QueueName);
            Console.WriteLine("Messages from queue '{0}' output to directory");
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
