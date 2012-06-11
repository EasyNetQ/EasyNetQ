using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.InMemoryClient
{
    public class BindingInfo
    {
        public QueueInfo Queue { get; private set; }
        public string RoutingKey { get; private set; }

        public BindingInfo(QueueInfo queue, string routingKey)
        {
            Queue = queue;
            RoutingKey = routingKey;
        }

        public bool RoutingKeyMatches(string messageRouting)
        {
            return Match(RoutingKey, messageRouting);
        }

        public static bool Match(string queue, string key)
        {
            var queueParts = queue.Split('.');

            CharMatchBase previous = null;
            IMatchChar first = null;
            for (int i = 0; i < queueParts.Length; i++)
            {
                switch (queueParts[i])
                {
                    case "*":
                        previous = new StarMatch(previous);
                        break;
                    case "#":
                        previous = new HashMatch(previous);
                        break;
                    default:
                        previous = new CharMatch(previous, queueParts[i]);
                        break;
                }
                if (i == 0) first = previous;
            }

            var keyParts = new LinkedList<string>(key.Split('.'));

            if (first == null) return false;

            first.Consume(keyParts);

            return first.MatchValues.All(x => x);
        }
    }

    public interface IMatchChar
    {
        void Consume(LinkedList<string> keyParts);
        bool Matched { get; }
        bool Matches(LinkedList<string> keyParts);
        IMatchChar Next { get; }
        IEnumerable<bool> MatchValues { get; }
    }

    public abstract class CharMatchBase : IMatchChar
    {
        protected CharMatchBase(CharMatchBase previous)
        {
            if (previous != null)
            {
                previous.Next = this;
            }
        }

        public void Consume(LinkedList<string> keyParts)
        {
            if (!Matched)
            {
                Matched = Matches(keyParts);
            }

            if (Next == null)
            {
                if (keyParts.Count > 0) Matched = false;
                return;
            }

            if (keyParts.Count > 0) Next.Consume(keyParts);
        }

        public IEnumerable<bool> MatchValues
        {
            get
            {
                yield return Matched;
                if (Next == null) yield break;

                foreach (var matchValue in Next.MatchValues)
                {
                    yield return matchValue;
                }
            }
        }

        public abstract bool Matches(LinkedList<string> keyParts);

        public bool Matched { get; protected set; }

        public IMatchChar Next { get; private set; }
    }

    public class CharMatch : CharMatchBase
    {
        private readonly string keyPart;

        public CharMatch(CharMatchBase previous, string keyPart)
            : base(previous)
        {
            this.keyPart = keyPart;
        }

        public override bool Matches(LinkedList<string> keyParts)
        {
            Matched = keyParts.First.Value == keyPart;
            keyParts.RemoveFirst();
            return Matched;
        }
    }

    public class StarMatch : CharMatchBase
    {
        public StarMatch(CharMatchBase previous)
            : base(previous)
        {
        }

        public override bool Matches(LinkedList<string> keyParts)
        {
            keyParts.RemoveFirst();
            return Matched = true;
        }
    }

    public class HashMatch : CharMatchBase
    {
        public HashMatch(CharMatchBase previous)
            : base(previous)
        {
        }

        public override bool Matches(LinkedList<string> keyParts)
        {
            if (Next == null)
            {
                keyParts.Clear();
                return Matched = true;
            }

            while (!Next.Matches(keyParts))
            {
                if (keyParts.Count == 0) return Matched = false;
            }
            return Matched = true;
        }
    }
}