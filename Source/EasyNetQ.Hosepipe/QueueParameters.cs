namespace EasyNetQ.Hosepipe;

public class QueueParameters
{
    public string HostName { get; set; } = "localhost";
    public int HostPort { get; set; } = -1;
    public string VHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; }
    public bool Purge { get; set; } = false;
    public int NumberOfMessagesToRetrieve { get; set; } = 1000;
    public string MessagesOutputDirectory { get; set; } = Directory.GetCurrentDirectory();
    public TimeSpan ConfirmsTimeout { get; } = TimeSpan.FromSeconds(30);
    public bool Ssl { get; set; } = false;

    // set some defaults
}
