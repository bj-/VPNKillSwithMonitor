namespace VKS.Contracts.Models;

public sealed record ProcessInfo
{
    public int ProcessId { get; init; }
    public string Name { get; init; } = "";
    public string? ExecutablePath { get; init; }
    public string? UserName { get; init; }
    public DateTimeOffset StartTime { get; init; }
}
