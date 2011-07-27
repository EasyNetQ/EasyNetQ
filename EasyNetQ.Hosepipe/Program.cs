using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace EasyNetQ.Hosepipe
{
    public class Program
    {
        static readonly ArgParser argParser = new ArgParser();

        public static void Main(string[] args)
        {
            var arguments = argParser.Parse(args);
            PrintUsage();
        }

        public static void PrintUsage()
        {
            using (var manifest = Assembly.GetExecutingAssembly().GetManifestResourceStream("EasyNetQ.Hosepipe.Usage.txt"))
            {
                if(manifest == null)
                {
                    throw new Exception("Could not load usage");
                }

//                var memoryStream = new MemoryStream();
//                manifest.CopyTo(memoryStream);

                var memoryStream = new MemoryStream();
                var buffer = Encoding.UTF8.GetBytes("Hello from the memory stream");
                memoryStream.Write(buffer, 0, buffer.Length);

                Console.WriteLine("memoryStream.Length = {0}", memoryStream.Length);

                using (var console = Console.OpenStandardOutput())
                {
                    // manifest.CopyTo(console);
                    memoryStream.CopyTo(console);
                    console.Position = 0;
                    console.Flush();
                }
            }
        }
    }
}
