using System.Threading.Tasks;

namespace minecraft_windows_service_wrapper.Services
{
    public interface IJavaVersionService
    {
        Task<int> GetJavaVersionAsync(string javaExecutablePath);
        Task<string> GetJavaHomeAsync();
        Task<string> GetJavaExecutablePathAsync(string javaHome);
        void ClearCache();
    }
}