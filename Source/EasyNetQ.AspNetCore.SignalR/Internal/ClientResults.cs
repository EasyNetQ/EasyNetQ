using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace EasyNetQ.AspNetCore.SignalR.Internal;

/// <summary>
///     Client results (<c>InvokeConnectionAsync</c>) in both roles: invocations this server is waiting for, and
///     invocations it delivered on behalf of another server, whose results it forwards there. The binder hands the
///     hub protocol the awaited type, or <see cref="RawResult" /> for forwarded ones so their payload stays untouched.
/// </summary>
internal sealed class ClientResults : IInvocationBinder
{
    private readonly ConcurrentDictionary<string, PendingResult> pending = new();
    private readonly ConcurrentDictionary<string, ForwardedInvocation> forwarded = new();

    public Task<T> Add<T>(string connectionId, string invocationId, CancellationToken cancellationToken)
    {
        var result = new PendingResult<T>(connectionId);
        pending[invocationId] = result;
        if (cancellationToken.CanBeCanceled)
            result.Registration = cancellationToken.Register(
                static state =>
                {
                    var (self, id) = ((ClientResults, string))state!;
                    if (self.pending.TryRemove(id, out var removed))
                        removed.Cancel();
                },
                (this, invocationId)
            );
        return result.Task;
    }

    public void Remove(string invocationId)
    {
        if (pending.TryRemove(invocationId, out var removed))
            removed.Dispose();
    }

    /// <summary>Completes an invocation this server awaits; only the connection it was sent to may answer</summary>
    public bool TryComplete(string? connectionId, CompletionMessage completion)
    {
        if (completion.InvocationId is not { } invocationId || !pending.TryGetValue(invocationId, out var result))
            return false;
        if (connectionId is not null && result.ConnectionId != connectionId)
            return false;
        if (!pending.TryRemove(invocationId, out result))
            return false;
        result.Complete(completion);
        return true;
    }

    public void AddForwarded(string invocationId, ForwardedInvocation invocation) => forwarded[invocationId] = invocation;

    public bool TryTakeForwarded(string connectionId, string invocationId, [MaybeNullWhen(false)] out ForwardedInvocation invocation)
    {
        if (forwarded.TryGetValue(invocationId, out invocation) && invocation.ConnectionId == connectionId)
            return forwarded.TryRemove(invocationId, out invocation);
        invocation = default;
        return false;
    }

    /// <summary>Fails what a disconnected connection still owes; returns the forwarded ones to answer remotely</summary>
    public List<KeyValuePair<string, ForwardedInvocation>> Disconnected(string connectionId)
    {
        foreach (var (invocationId, result) in pending)
            if (result.ConnectionId == connectionId && pending.TryRemove(invocationId, out var removed))
                removed.Fail(new IOException($"Connection '{connectionId}' disconnected."));

        var orphaned = new List<KeyValuePair<string, ForwardedInvocation>>();
        foreach (var entry in forwarded)
            if (entry.Value.ConnectionId == connectionId && forwarded.TryRemove(entry.Key, out var invocation))
                orphaned.Add(new KeyValuePair<string, ForwardedInvocation>(entry.Key, invocation));
        return orphaned;
    }

    public void FailAll(Exception exception)
    {
        foreach (var invocationId in pending.Keys)
            if (pending.TryRemove(invocationId, out var removed))
                removed.Fail(exception);
        forwarded.Clear();
    }

    public bool TryGetReturnType(string invocationId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Type? type)
    {
        if (pending.TryGetValue(invocationId, out var result))
        {
            type = result.ResultType;
            return true;
        }
        if (forwarded.ContainsKey(invocationId))
        {
            type = typeof(RawResult);
            return true;
        }
        type = null;
        return false;
    }

    Type IInvocationBinder.GetReturnType(string invocationId)
        => TryGetReturnType(invocationId, out var type) ? type : throw new InvalidOperationException($"Unknown invocation '{invocationId}'.");

    IReadOnlyList<Type> IInvocationBinder.GetParameterTypes(string methodName) => throw new NotSupportedException();

    Type IInvocationBinder.GetStreamItemType(string streamId) => throw new NotSupportedException();

    internal sealed record ForwardedInvocation(string ConnectionId, string ReturnServer, IHubProtocol Protocol);

    private abstract class PendingResult : IDisposable
    {
        protected PendingResult(string connectionId) => ConnectionId = connectionId;

        public string ConnectionId { get; }
        public CancellationTokenRegistration Registration { get; set; }
        public abstract Type ResultType { get; }
        public abstract void Complete(CompletionMessage completion);
        public abstract void Fail(Exception exception);
        public abstract void Cancel();
        public void Dispose() => Registration.Dispose();
    }

    private sealed class PendingResult<T> : PendingResult
    {
        private readonly TaskCompletionSource<T> source = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingResult(string connectionId) : base(connectionId)
        {
        }

        public Task<T> Task => source.Task;
        public override Type ResultType => typeof(T);

        public override void Complete(CompletionMessage completion)
        {
            Dispose();
            if (completion.Error is not null)
                source.TrySetException(new HubException(completion.Error));
            else if (completion.Result is T value)
                source.TrySetResult(value);
            else if (completion.Result is null && default(T) is null)
                source.TrySetResult(default!);
            else
                source.TrySetException(new InvalidCastException(
                    $"Client result of type {completion.Result?.GetType().Name ?? "null"} is not a {typeof(T).Name}."
                ));
        }

        public override void Fail(Exception exception)
        {
            Dispose();
            source.TrySetException(exception);
        }

        public override void Cancel()
        {
            Dispose();
            source.TrySetCanceled();
        }
    }
}
