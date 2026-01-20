namespace EasyNetQ.IntegrationTests.Rpc;

public record Request
{
    public Request(int id)
    {
        Id = id;
    }

    public int Id { get; }
}

public record BunnyRequest : Request
{
    public BunnyRequest(int id) : base(id)
    {
    }
}


public record RabbitRequest : Request
{
    public RabbitRequest(int id) : base(id)
    {
    }
}
[Queue(Type = QueueType.Quorum)]
public record RabbitQuorumRequest : Request
{
    public RabbitQuorumRequest(int id) : base(id)
    {
    }
}

public record Response
{
    public Response(int id)
    {
        Id = id;
    }

    public int Id { get; }

}


public record BunnyResponse : Response
{
    public BunnyResponse(int id) : base(id)
    {
    }
}

public record RabbitResponse : Response
{
    public RabbitResponse(int id) : base(id)
    {
    }
}
[Queue(Type = QueueType.Quorum)]
public record RabbitQuorumResponse : Response
{
    public RabbitQuorumResponse(int id) : base(id)
    {
    }
}
[Serializable]
public class RequestFailedException : Exception
{
    public RequestFailedException()
    {
    }

    public RequestFailedException(string message) : base(message)
    {
    }

    public RequestFailedException(string message, Exception inner) : base(message, inner)
    {
    }
}
