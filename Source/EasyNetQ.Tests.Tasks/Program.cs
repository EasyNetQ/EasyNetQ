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
