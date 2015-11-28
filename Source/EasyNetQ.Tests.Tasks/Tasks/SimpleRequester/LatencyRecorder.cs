using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EasyNetQ.Tests.SimpleRequester
{
    public class LatencyRecorder : ILatencyRecorder
    {
        private readonly IDictionary<long, RequestRecord> requests = 
            new ConcurrentDictionary<long, RequestRecord>();

        private readonly Timer reportTimer;

        private readonly TimeSpan reportInterval;

        public LatencyRecorder()
        {
            reportInterval = TimeSpan.FromSeconds(10);
            reportTimer = new Timer(Report, null, reportInterval, reportInterval);
        }

        public void Dispose()
        {
            reportTimer.Dispose();
        }

        public void RegisterRequest(long requestId)
        {
            requests.Add(requestId, new RequestRecord(requestId));
        }

        public void RegisterResponse(long responseId)
        {
            if (!requests.ContainsKey(responseId))
            {
                // see if it turns up
                Thread.Sleep(100);
                if (!requests.ContainsKey(responseId))
                {
                    Console.WriteLine("Response contains unknown key: {0}", responseId);
                    return;
                }
            }
            requests[responseId].Respond();
        }

        public void Report(object status)
        {
            var ticksTenSecondsAgo = DateTime.Now.AddSeconds(-10).Ticks;
            var lateResponses = requests.Where(x => (!x.Value.HasResponded) && (x.Value.Ticks < ticksTenSecondsAgo));

            var reponded = requests.Count(x => x.Value.HasResponded);

            Console.WriteLine("Total: {0}, reponded: {1} over 10 seconds late: [{2}]", 
                requests.Count,
                reponded,
                string.Join(",", lateResponses.Select(x => x.Value.Id.ToString())));
        }
    }

    public class RequestRecord
    {
        public RequestRecord(long id)
        {
            Id = id;
            Ticks = DateTime.Now.Ticks;
        }

        public void Respond()
        {
            HasResponded = true;
            ResponseTimeTicks = DateTime.Now.Ticks - Ticks;
        }

        public long Id { get; private set; }
        public long Ticks { get; private set; }
        public bool HasResponded { get; private set; }
        public long ResponseTimeTicks { get; private set; }
    }
}