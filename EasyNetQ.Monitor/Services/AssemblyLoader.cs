using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyNetQ.Monitor.Services
{
    public class AssemblyLoader
    {
        public IEnumerable<string> GetTypes(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return assembly.GetTypes().Select(TypeNameSerializer.Serialize);
        }
    }

    public class AssemblyLoaderTests
    {
        public void ShouldBeAbleToGetTypes()
        {
            var assemblyLoader = new AssemblyLoader();
            var types = assemblyLoader.GetTypes(@"C:\Source\Mike.AmqpSpike\EasyNetQ.Tests.Messages\bin\Debug\EasyNetQ.Tests.Messages.dll");
            foreach (var type in types)
            {
                Console.WriteLine(type);
            }
        }
    }
}