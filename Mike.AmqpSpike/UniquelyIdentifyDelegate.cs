using System;

namespace Mike.AmqpSpike
{
    public class UniquelyIdentifyDelegate
    {
        public void CanItBeDone()
        {
            DoSomethingLater(() => {});
        }

        public void DoSomethingLater(Action action)
        {
            var typeName = action.Method.DeclaringType + "_" + action.Method.Name;
            Console.WriteLine(typeName);
        }
    }
}