using System;

namespace Mike.AmqpSpike
{
    public class LambdaSpike
    {
        public void Test()
        {
            var name = " Mike";
            RunMe(message => Console.WriteLine(message + name));
        }

        public void Print(string message)
        {
            Console.WriteLine(message);
        }

        public void RunMe(Action<string> myAction)
        {
            myAction("Hello");
        }
    }
}