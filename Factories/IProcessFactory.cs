using System.Diagnostics;
using System.Threading.Tasks;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Factories
{
    public interface IProcessFactory
    {
        Task<ProcessStartInfo> CreateMinecraftServerProcessAsync(MinecraftServerOptions options);
        ProcessStartInfo CreateJavaVersionDetectionProcess(string javaExecutablePath);
    }
}