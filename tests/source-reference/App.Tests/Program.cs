using EasyNetQ.Transport.InMemory;

Console.WriteLine(new InMemoryTransport().GetType().Name + " " + typeof(Ping).Name);
