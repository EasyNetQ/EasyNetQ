namespace EasyNetQ.DI.Tests;

public delegate IServiceResolver ResolverFactory(Action<IServiceRegister> configure);
