using System;
using System.Collections.Generic;
using System.Threading;

namespace EasyNetQ.Tests.SimpleRequester
{
    public class LatencyRecorder : ILatencyRecorder
    {
        private ISet<long> currentSet = new HashSet<long>();
        private ISet<long> previousSet = new HashSet<long>();
        private readonly object responseSetLock = new object();
        private readonly Timer reportTimer;

        private const int reportIntervalSeconds = 10;
        private const int milliseconds = 1000;
        private const int reportIntervalMilliseconds = reportIntervalSeconds*milliseconds;

        public LatencyRecorder()
        {
            reportTimer = new Timer(x => Report(), null, reportIntervalMilliseconds, reportIntervalMilliseconds);
        }

        public void RegisterRequest(long requestId)
        {
            lock (responseSetLock)
            {
                currentSet.Add(requestId);
            }
        }

        public void RegisterResponse(long responseId)
        {
            lock (responseSetLock)
            {
                if (!currentSet.Remove(responseId))
                {
                    if (!previousSet.Remove(responseId))
                    {
                        Console.WriteLine("Got late message {0}", responseId);
                    }
                }
            }
        }

        public void Report()
        {
            lock (responseSetLock)
            {
                foreach (var timedoutId in previousSet)
                {
                    Console.WriteLine("Missing response from message {0}", timedoutId);
                }
                previousSet = currentSet;
                currentSet = new ConcurrentHashSet<long>();
            }
        }

        public void Dispose()
        {
            reportTimer.Dispose();
        }
    }
}