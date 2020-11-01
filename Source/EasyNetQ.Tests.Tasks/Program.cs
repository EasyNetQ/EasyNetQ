using System;

namespace EasyNetQ.Tests.Tasks
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            using (var interactiveTaskRunner = new CommandLineTaskRunner())
            {
                try
                {
                    return interactiveTaskRunner.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.ReadLine();
                    return 1;
                }
            }
        }
    }
}
