using System;
using System.Runtime.Serialization;

namespace EasyNetQ.IntegrationTests.Rpc
{
    public class Request
    {
        public Request(int id)
        {
            Id = id;
        }

        public int Id { get; }

        protected bool Equals(Request other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Request) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class BunnyRequest : Request
    {
        public BunnyRequest(int id) : base(id)
        {
        }
    }

    public class RabbitRequest : Request
    {
        public RabbitRequest(int id) : base(id)
        {
        }
    }

    public class Response
    {
        public Response(int id)
        {
            Id = id;
        }

        public int Id { get; }

        protected bool Equals(Response other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Response) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }


    public class BunnyResponse : Response
    {
        public BunnyResponse(int id) : base(id)
        {
        }
    }

    public class RabbitResponse : Response
    {
        public RabbitResponse(int id) : base(id)
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

        protected RequestFailedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
