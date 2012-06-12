using System;

namespace EasyNetQ.InMemoryClient
{
    public class CircleBuffer<T>
    {
        private CircleBufferNode<T> current;

        public void Add(T item)
        {
            current = 
                current == null ? 
                new CircleBufferNode<T>(item) : 
                new CircleBufferNode<T>(item, current);
        }

        public T Next
        {
            get
            {
                if (current == null)
                {
                    throw new ApplicationException("Circle Buffer Is Empty. Add some items before using Next");
                }

                current = current.Next;
                return current.Value;
            }
        }
    }

    public class CircleBufferNode<T>
    {
        public T Value { get; private set; }
        public CircleBufferNode<T> Next { get; private set; }

        public CircleBufferNode(T value, CircleBufferNode<T> previous)
        {
            Value = value;
            Next = previous.Next;
            previous.Next = this;
        }

        public CircleBufferNode(T value)
        {
            Value = value;
            Next = this;
        }
    }

    public class CircleBufferTests
    {
        public void TryIt()
        {
            var circleBuffer = new CircleBuffer<string>();
            circleBuffer.Add("one");
            circleBuffer.Add("two");
            circleBuffer.Add("three");

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(circleBuffer.Next);
            }
        }
    }
}