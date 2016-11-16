using System;
using System.IO;

namespace EasyNetQ.Hosepipe
{
    public class QueueParameters
    {
        public string HostName { get; set; }
        public string VHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string QueueName { get; set; }
        public bool Purge { get; set; }
        public int NumberOfMessagesToRetrieve { get; set; }
        public string MessageFilePath { get; set; }

        public QueueParameters()
        {
            // set some defaults
            HostName = "localhost";
            VHost = "/";
            Username = "guest";
            Password = "guest";
            Purge = false;
            NumberOfMessagesToRetrieve = 1000;
            MessageFilePath = Directory.GetCurrentDirectory();
        }
    }
}