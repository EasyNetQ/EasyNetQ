namespace EasyNetQ.Monitor.Dialogue
{
    public class FromDialogue<T>
    {
        public T Value { get; private set; }
        public bool WasCancelled { get; private set; }

        private FromDialogue(){}
        public static FromDialogue<T> OK(T value)
        {
            return new FromDialogue<T>{ Value = value, WasCancelled = false };
        }
        public static FromDialogue<T> Cancelled()
        {
            return new FromDialogue<T> {Value = default(T), WasCancelled = true};
        }
    }
}