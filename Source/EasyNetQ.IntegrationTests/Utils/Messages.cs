namespace EasyNetQ.IntegrationTests.Utils
{
    public class Message
    {
        public Message(int id)
        {
            Id = id;
        }

        public int Id { get; }

        protected bool Equals(Message other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Message) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public class RabbitMessage : Message
    {
        public RabbitMessage(int id) : base(id)
        {
        }
    }

    public class BunnyMessage : Message
    {
        public BunnyMessage(int id) : base(id)
        {
        }
    }
}
