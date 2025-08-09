using System.Collections.Generic;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Strategies.Java
{
    public interface IJavaVersionStrategy
    {
        int SupportedJavaVersion { get; }
        IEnumerable<string> GetMemoryArguments(MinecraftServerOptions options);
        IEnumerable<string> GetGarbageCollectionArguments();
        IEnumerable<string> GetAdditionalArguments();
    }
}