namespace VKS.Core.Configuration;

public interface IOptionsProvider
{
    VksOptions Current { get; }
}
