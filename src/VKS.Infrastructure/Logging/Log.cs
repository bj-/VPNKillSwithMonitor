namespace VKS.Infrastructure.Logging;

public static class Log
{
    // Replace with structured logging (e.g. ILogger/Serilog) in the service host.
    public static void SecurityEvent(string eventName, string details)
        => Console.WriteLine($"{DateTimeOffset.UtcNow:O} [{eventName}] {details}");
}
