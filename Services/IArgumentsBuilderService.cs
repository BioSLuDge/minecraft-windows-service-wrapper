using System.Collections.Generic;

namespace minecraft_windows_service_wrapper.Services
{
    public interface IArgumentsBuilderService
    {
        IEnumerable<string> BuildJavaArguments(int javaVersion, string serverJarPath, CommandLineOptions options);
        IEnumerable<string> BuildMinecraftArguments(CommandLineOptions options);
        IEnumerable<string> BuildAllArguments(int javaVersion, string serverJarPath, CommandLineOptions options);
    }
}