using System.Diagnostics;

namespace minecraft_windows_service_wrapper.Services
{
    public static class WindowsServiceManager
    {
        public static void Install(string serviceName, string exePath)
        {
            var psi = new ProcessStartInfo("sc.exe", $"create \"{serviceName}\" binPath= \"{exePath}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit();
        }

        public static void Remove(string serviceName)
        {
            var psi = new ProcessStartInfo("sc.exe", $"delete \"{serviceName}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit();
        }
    }
}
