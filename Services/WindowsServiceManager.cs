using System;
using System.Diagnostics;

namespace minecraft_windows_service_wrapper.Services
{
    public static class WindowsServiceManager
    {
        public static void Install(string serviceName, string exePath, IProcessRunner? processRunner = null)
        {
            var psi = new ProcessStartInfo("sc.exe", $"create \"{serviceName}\" binPath= \"{exePath}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            processRunner ??= new ProcessRunner();
            var result = processRunner.Run(psi);

            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                Console.WriteLine(result.StandardOutput);
            if (!string.IsNullOrWhiteSpace(result.StandardError))
                Console.Error.WriteLine(result.StandardError);

            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Service installation failed with exit code {result.ExitCode}");
        }

        public static void Remove(string serviceName, IProcessRunner? processRunner = null)
        {
            var psi = new ProcessStartInfo("sc.exe", $"delete \"{serviceName}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            processRunner ??= new ProcessRunner();
            var result = processRunner.Run(psi);

            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                Console.WriteLine(result.StandardOutput);
            if (!string.IsNullOrWhiteSpace(result.StandardError))
                Console.Error.WriteLine(result.StandardError);

            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Service removal failed with exit code {result.ExitCode}");
        }
    }
}
