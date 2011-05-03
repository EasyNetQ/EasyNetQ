using System;

namespace EasyNetQ.Tests
{
    public class DelegateHashCodeSpike
    {
        public void IsDelegateHashCodeReliable()
        {
            Func<int, int> add = x => x + 2;
            Func<int, int> add2 = x => x + 2;
            Func<int, int> mult = x => x*2;
            Func<string, string> hello = x => "Hello " + x;

            Console.Out.WriteLine("add.GetHashCode() = {0}", add.Method.GetHashCode());
            Console.Out.WriteLine("add.GetHashCode() = {0}", add.Method.GetHashCode());

            Console.Out.WriteLine("add2.GetHashCode() = {0}", add2.Method.GetHashCode());
            Console.Out.WriteLine("mult.GetHashCode() = {0}", mult.Method.GetHashCode());
            Console.Out.WriteLine("hello.GetHashCode() = {0}", hello.Method.GetHashCode());
        }
    }
}