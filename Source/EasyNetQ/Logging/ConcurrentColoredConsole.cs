namespace EasyNetQ.Logging;

internal static class ConcurrentColoredConsole
{
    private static readonly object Mutex = new();

    public static void WriteLine(ConsoleColor color, string value)
    {
        lock (Mutex)
        {
            var originalForeground = Console.ForegroundColor;
            Console.ForegroundColor = color;
            try
            {
                Console.WriteLine(value);
            }
            finally
            {
                Console.ForegroundColor = originalForeground;
            }
        }
    }
}
