using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyNetQ.Tests.Tasks
{
    class Program
    {
        static int Main(string[] args)
        {
            using (var interactiveTaskRunner = new CommandLineTaskRunner())
            {
                try
                {
                    var main = interactiveTaskRunner.Run();
                    Console.ReadLine();
                    return main;
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
