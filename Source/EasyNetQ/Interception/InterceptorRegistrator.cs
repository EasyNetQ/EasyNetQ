using EasyNetQ.DI;

namespace EasyNetQ.Interception;

/// <summary>
///     Helper interface that allows to register interceptors in DI
/// </summary>
public interface IInterceptorRegistrator
{
    /// <summary>
    /// Registers an instance of <see cref="IProduceConsumeInterceptor"/> interceptor
    /// </summary>
    void Add(IProduceConsumeInterceptor interceptor);

    /// <summary>
    /// Registers an interceptor of the specified type <typeparamref name="TInterceptor"/>
    /// </summary>
    void Add<TInterceptor>() where TInterceptor : class, IProduceConsumeInterceptor;
}

internal sealed class InterceptorRegistrator : IInterceptorRegistrator
{
    private readonly IServiceRegister serviceRegister;

    public InterceptorRegistrator(IServiceRegister serviceRegister)
    {
        this.serviceRegister = serviceRegister;
    }

    public void Add(IProduceConsumeInterceptor interceptor)
    {
        serviceRegister.Register(interceptor); // TODO: change to collection register
    }

    /// <inheritdoc />
    public void Add<TInterceptor>() where TInterceptor : class, IProduceConsumeInterceptor
    {
        serviceRegister.Register<IProduceConsumeInterceptor, TInterceptor>(); // TODO: change to collection register
    }
}
