using System;

namespace EasyNetQ.Tests.Integration.Scheduling
{
    [Serializable]
    public class PartyInvitation
    {
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }
}