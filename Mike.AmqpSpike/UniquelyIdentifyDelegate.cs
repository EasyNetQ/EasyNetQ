using System;
using System.Collections.Generic;

namespace Mike.AmqpSpike
{
    public class UniquelyIdentifyDelegate
    {
        readonly IDictionary<int, Action> actionCache = new Dictionary<int, Action>();

        /// <summary>
        /// The really cool thing here, is that we can identify each anonymous delegate created
        /// by the compiler by its hash code. However, any closed-over variables from the outer
        /// scope still work as expected :)
        /// </summary>
        public void DemonstrateActionCache()
        {
            for (var i=0; i < 3; i++)
            {
                RunAction(() => Console.Out.WriteLine("Hello from A {0}", i));
                RunAction(() => Console.Out.WriteLine("Hello from B {0}", i));

                Console.Out.WriteLine("");
            }
        }

        public void RunAction(Action action)
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