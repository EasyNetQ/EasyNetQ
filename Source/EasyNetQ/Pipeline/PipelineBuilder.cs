using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Pipeline;

/// <summary>
///     Composes middleware into a <see cref="PipelineStep{TContext}" />. Registration happens at configuration time;
///     <see cref="Build" /> resolves every middleware once and chains them, so nothing is resolved per message.
///     Middleware is identified by its type (or a name for inline steps), which lets configuration replace, remove
///     or insert around the built-in steps.
/// </summary>
public sealed class PipelineBuilder<TContext> where TContext : LayerContext
{
    private readonly List<Registration> registrations;

    /// <summary>
    ///     Creates an empty builder
    /// </summary>
    public PipelineBuilder()
    {
        registrations = new List<Registration>();
    }

    private PipelineBuilder(List<Registration> registrations)
    {
        this.registrations = registrations;
    }

    /// <summary>
    ///     Names of the registered steps, in order
    /// </summary>
    public IReadOnlyList<string> Steps => registrations.ConvertAll(static r => r.Name);

    /// <summary>
    ///     Appends a middleware instance
    /// </summary>
    public PipelineBuilder<TContext> Use(IMiddleware<TContext> middleware, string? name = null)
        => Append(new Registration(middleware.GetType(), name ?? middleware.GetType().Name, _ => middleware));

    /// <summary>
    ///     Appends a middleware resolved from the service provider at build time
    /// </summary>
    public PipelineBuilder<TContext> Use<TMiddleware>() where TMiddleware : class, IMiddleware<TContext>
        => Append(new Registration(typeof(TMiddleware), typeof(TMiddleware).Name, static services => services.GetRequiredService<TMiddleware>()));

    /// <summary>
    ///     Appends a middleware created by <paramref name="factory" /> at build time
    /// </summary>
    public PipelineBuilder<TContext> Use<TMiddleware>(Func<IServiceProvider, TMiddleware> factory) where TMiddleware : class, IMiddleware<TContext>
        => Append(new Registration(typeof(TMiddleware), typeof(TMiddleware).Name, factory));

    /// <summary>
    ///     Appends an inline step identified by <paramref name="name" />
    /// </summary>
    public PipelineBuilder<TContext> Use(string name, Func<TContext, PipelineStep<TContext>, ValueTask> step)
        => Append(new Registration(null, name, _ => new DelegateMiddleware(step)));

    /// <summary>
    ///     Replaces the middleware registered as <typeparamref name="TMarker" />
    /// </summary>
    public PipelineBuilder<TContext> Replace<TMarker>(IMiddleware<TContext> replacement) where TMarker : IMiddleware<TContext>
    {
        var index = IndexOf(typeof(TMarker));
        registrations[index] = new Registration(replacement.GetType(), replacement.GetType().Name, _ => replacement);
        return this;
    }

    /// <summary>
    ///     Inserts a middleware before the one registered as <typeparamref name="TMarker" />
    /// </summary>
    public PipelineBuilder<TContext> InsertBefore<TMarker>(IMiddleware<TContext> middleware) where TMarker : IMiddleware<TContext>
    {
        registrations.Insert(IndexOf(typeof(TMarker)), new Registration(middleware.GetType(), middleware.GetType().Name, _ => middleware));
        return this;
    }

    /// <summary>
    ///     Inserts a middleware after the one registered as <typeparamref name="TMarker" />
    /// </summary>
    public PipelineBuilder<TContext> InsertAfter<TMarker>(IMiddleware<TContext> middleware) where TMarker : IMiddleware<TContext>
    {
        registrations.Insert(IndexOf(typeof(TMarker)) + 1, new Registration(middleware.GetType(), middleware.GetType().Name, _ => middleware));
        return this;
    }

    /// <summary>
    ///     Removes the middleware registered as <typeparamref name="TMarker" />
    /// </summary>
    public PipelineBuilder<TContext> Remove<TMarker>() where TMarker : IMiddleware<TContext>
    {
        registrations.RemoveAt(IndexOf(typeof(TMarker)));
        return this;
    }

    /// <summary>
    ///     Removes the inline step registered as <paramref name="name" />
    /// </summary>
    public PipelineBuilder<TContext> Remove(string name)
    {
        var index = registrations.FindIndex(r => r.Name == name);
        if (index < 0) throw new InvalidOperationException($"No pipeline step named '{name}' is registered");
        registrations.RemoveAt(index);
        return this;
    }

    /// <summary>
    ///     Copies the builder so a child scope can add steps without affecting the parent
    /// </summary>
    public PipelineBuilder<TContext> Clone() => new(new List<Registration>(registrations));

    /// <summary>
    ///     Resolves every middleware and chains them, ending in <paramref name="terminal" /> (or a no-op)
    /// </summary>
    public PipelineStep<TContext> Build(IServiceProvider services, PipelineStep<TContext>? terminal = null)
    {
        PipelineStep<TContext> step = terminal ?? (static _ => default);
        for (var i = registrations.Count - 1; i >= 0; i--)
        {
            var middleware = registrations[i].Factory(services);
            var next = step;
            step = context => middleware.InvokeAsync(context, next);
        }

        return step;
    }

    private PipelineBuilder<TContext> Append(in Registration registration)
    {
        registrations.Add(registration);
        return this;
    }

    private int IndexOf(Type marker)
    {
        var index = registrations.FindIndex(r => r.Marker == marker);
        if (index < 0) throw new InvalidOperationException($"No pipeline step of type '{marker.Name}' is registered");
        return index;
    }

    private readonly record struct Registration(Type? Marker, string Name, Func<IServiceProvider, IMiddleware<TContext>> Factory);

    private sealed class DelegateMiddleware : IMiddleware<TContext>
    {
        private readonly Func<TContext, PipelineStep<TContext>, ValueTask> step;

        public DelegateMiddleware(Func<TContext, PipelineStep<TContext>, ValueTask> step) => this.step = step;

        public ValueTask InvokeAsync(TContext context, PipelineStep<TContext> next) => step(context, next);
    }
}
