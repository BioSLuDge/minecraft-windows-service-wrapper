using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace minecraft_windows_service_wrapper.Services
{
    public class JavaVersionService : IJavaVersionService
    {
        private readonly ILogger<JavaVersionService> _logger;
        private readonly ConcurrentDictionary<string, int> _versionCache = new();

        public JavaVersionService(ILogger<JavaVersionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> GetJavaVersionAsync(string javaExecutablePath)
        {
            if (string.IsNullOrWhiteSpace(javaExecutablePath))
                throw new ArgumentException("Java executable path cannot be null or empty", nameof(javaExecutablePath));

            if (!File.Exists(javaExecutablePath))
                throw new FileNotFoundException($"Java executable not found at: {javaExecutablePath}");

            if (_versionCache.TryGetValue(javaExecutablePath, out var cachedVersion))
            {
                _logger.LogDebug("Using cached Java version {Version} for {Path}", cachedVersion, javaExecutablePath);
                return cachedVersion;
            }

            var version = await DetectJavaVersionAsync(javaExecutablePath);
            _versionCache.TryAdd(javaExecutablePath, version);
            
            _logger.LogInformation("Detected Java version {Version} for {Path}", version, javaExecutablePath);
            return version;
        }

        public Task<string> GetJavaHomeAsync()
        {
            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            
            if (string.IsNullOrWhiteSpace(javaHome))
                throw new InvalidOperationException("JAVA_HOME environment variable is not set");

            if (!Directory.Exists(javaHome))
                throw new DirectoryNotFoundException($"JAVA_HOME directory does not exist: {javaHome}");

            _logger.LogDebug("Using JAVA_HOME: {JavaHome}", javaHome);
            return Task.FromResult(javaHome);
        }

        public Task<string> GetJavaExecutablePathAsync(string javaHome)
        {
            if (string.IsNullOrWhiteSpace(javaHome))
                throw new ArgumentException("Java home cannot be null or empty", nameof(javaHome));

            var javaExe = Path.Combine(javaHome, "bin", "java.exe");
            
            if (!File.Exists(javaExe))
                throw new FileNotFoundException($"Java executable not found at: {javaExe}");

            return Task.FromResult(javaExe);
        }

        public void ClearCache()
        {
            _versionCache.Clear();
            _logger.LogDebug("Java version cache cleared");
        }

        private async Task<int> DetectJavaVersionAsync(string javaExecutablePath)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo(javaExecutablePath)
                {
                    Arguments = "-version",
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                process.Start();
                
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Java version detection failed with exit code {process.ExitCode}. Error: {error}");
                }

                return ParseJavaVersion(error.IsNullOrEmpty() ? output : error);
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Failed to detect Java version from {javaExecutablePath}", ex);
            }
        }

        private int ParseJavaVersion(string versionOutput)
        {
            if (string.IsNullOrWhiteSpace(versionOutput))
                throw new InvalidOperationException("Java version output is empty");

            var versionMatch = Regex.Match(versionOutput, @"[0-9]+\.[0-9]+\.[0-9]+");
            if (!versionMatch.Success)
            {
                throw new InvalidOperationException($"Could not parse Java version from output: {versionOutput}");
            }

            if (!Version.TryParse(versionMatch.Value, out var version))
            {
                throw new InvalidOperationException($"Invalid version format: {versionMatch.Value}");
            }

            if (version.Major == 1)
                return version.Minor;
            
            return version.Major;
        }
    }

    internal static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);
    }
}