using System.Text.Json;
using VKS.Core.Configuration;

namespace VKS.Infrastructure.Configuration;

public sealed class JsonOptionsProvider : IOptionsProvider
{
    public VksOptions Current { get; }

    public JsonOptionsProvider(string path)
    {
        if (!File.Exists(path))
        {
            Current = new VksOptions();
            return;
        }

        var json = File.ReadAllText(path);
        Current = JsonSerializer.Deserialize<VksOptions>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new VksOptions();
    }
}
