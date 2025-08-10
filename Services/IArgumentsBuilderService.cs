using System.Collections.Generic;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Services
{
    public interface IArgumentsBuilderService
    {
        IEnumerable<string> BuildJavaArguments(int javaVersion, string serverJarPath, MinecraftServerOptions options);
        IEnumerable<string> BuildMinecraftArguments(MinecraftServerOptions options);
        IEnumerable<string> BuildAllArguments(int javaVersion, string serverJarPath, MinecraftServerOptions options);
    }
}