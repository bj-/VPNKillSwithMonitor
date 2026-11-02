using VKS.Core.Configuration;

namespace VKS.ProcessMonitor;

public sealed class ProcessMatcher
{
    private readonly IReadOnlyList<BlockedProcessRule> _rules;
    public ProcessMatcher(IEnumerable<BlockedProcessRule> rules) => _rules = rules.ToArray();

    public bool IsBlocked(string processName, string? executablePath)
    {
        foreach (var rule in _rules)
        {
            if (!string.Equals(Path.GetFileName(processName), rule.Name,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Path.GetFileName(executablePath), rule.Name,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(rule.Path))
                return true;

            if (!string.IsNullOrWhiteSpace(executablePath) &&
                string.Equals(Path.GetFullPath(executablePath),
                    Path.GetFullPath(rule.Path), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
