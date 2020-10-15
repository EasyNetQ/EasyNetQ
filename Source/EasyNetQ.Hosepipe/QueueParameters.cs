using System;
using System.IO;

namespace EasyNetQ.Hosepipe
{
    public class QueueParameters
    {
        public string HostName { get; set; }
        public int HostPort { get; set; }
        public string VHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string QueueName { get; set; }
        public bool Purge { get; set; }
        public int NumberOfMessagesToRetrieve { get; set; }
        public string MessagesOutputDirectory { get; set; }
        public TimeSpan ConfirmsTimeout { get; }

        public QueueParameters()
        {
            // set some defaults
            HostName = "localhost";
            HostPort = -1;
            VHost = "/";
            Username = "guest";
            Password = "guest";
            Purge = false;
            NumberOfMessagesToRetrieve = 1000;
            MessagesOutputDirectory = Directory.GetCurrentDirectory();
            ConfirmsTimeout = TimeSpan.FromSeconds(30);
        }
    }
}
