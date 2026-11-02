namespace VKS.Contracts.Models;

public sealed record UnlockResult
{
    public bool Success { get; init; }
    public string? RejectionReason { get; init; }
    public SystemStatus? Status { get; init; }
}
