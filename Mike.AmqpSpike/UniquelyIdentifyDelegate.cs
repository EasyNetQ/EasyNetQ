using System;
using System.Collections.Generic;

namespace Mike.AmqpSpike
{
    public class UniquelyIdentifyDelegate
    {
        readonly IDictionary<int, Action> actionCache = new Dictionary<int, Action>();

        public UniquelyIdentifyDelegate()
        {
            Console.Out.WriteLine("constructor");
        }
        
        /// <summary>
        /// The really cool thing here, is that we can identify each anonymous delegate created
        /// by the compiler by its hash code. However, any closed-over variables from the outer
        /// scope still work as expected :)
        /// </summary>
        public void CanItBeDone()
        {
            for (var i=0; i < 3; i++)
            {
                DoSomethingLater(() => { });
                DoSomethingLater(() => Console.Out.WriteLine("Hello {0}", i));
                DoSomethingLater(() => Console.Out.WriteLine("Hello"));

                Console.Out.WriteLine("");
            }
        }

        public void DoSomethingLater(Action action)
        {
            Console.Out.WriteLine("Mehod = {0}, Cache Size = {1}", action.Method.GetHashCode(), actionCache.Count);
            if (!actionCache.ContainsKey(action.Method.GetHashCode()))
            {
                actionCache.Add(action.Method.GetHashCode(), action);
            }

            var actionFromCache = actionCache[action.Method.GetHashCode()];

            actionFromCache();
        }
    }
}