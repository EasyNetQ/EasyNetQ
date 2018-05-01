using System;

namespace EasyNetQ.DI
{
    public interface IServiceResolverScope : IServiceResolver, IDisposable
    {
    }
}