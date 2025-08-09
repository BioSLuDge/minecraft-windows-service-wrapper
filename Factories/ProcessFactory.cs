using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;
using minecraft_windows_service_wrapper.Strategies.Java;
using minecraft_windows_service_wrapper.Strategies.Minecraft;

namespace minecraft_windows_service_wrapper.Factories
{
    public class ProcessFactory : IProcessFactory
    {
        private readonly ILogger<ProcessFactory> _logger;
        private readonly IJavaVersionService _javaVersionService;
        private readonly IJavaVersionStrategyFactory _javaStrategyFactory;
        private readonly IMinecraftVersionStrategyFactory _minecraftStrategyFactory;

        public ProcessFactory(
            ILogger<ProcessFactory> logger,
            IJavaVersionService javaVersionService,
            IJavaVersionStrategyFactory javaStrategyFactory,
            IMinecraftVersionStrategyFactory minecraftStrategyFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _javaVersionService = javaVersionService ?? throw new ArgumentNullException(nameof(javaVersionService));
            _javaStrategyFactory = javaStrategyFactory ?? throw new ArgumentNullException(nameof(javaStrategyFactory));
            _minecraftStrategyFactory = minecraftStrategyFactory ?? throw new ArgumentNullException(nameof(minecraftStrategyFactory));
        }

        public async Task<ProcessStartInfo> CreateMinecraftServerProcessAsync(MinecraftServerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Get Java home and executable
            var javaHome = options.JavaHome ?? await _javaVersionService.GetJavaHomeAsync();
            var javaExe = await _javaVersionService.GetJavaExecutablePathAsync(javaHome);
            
            // Detect Java version
            var javaVersion = await _javaVersionService.GetJavaVersionAsync(javaExe);
            
            // Get server JAR path
            var serverJarPath = Path.Combine(options.ServerDirectory, options.JarFileName);
            if (!File.Exists(serverJarPath))
                throw new FileNotFoundException($"Server JAR file not found: {serverJarPath}");

            // Create process start info
            var processStartInfo = new ProcessStartInfo(javaExe)
            {
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = options.ServerDirectory,
            };

            // Set JAVA_HOME environment variable if specified
            if (!string.IsNullOrWhiteSpace(options.JavaHome))
            {
                // Use indexer assignment instead of Add to avoid exceptions when JAVA_HOME already exists
                processStartInfo.Environment["JAVA_HOME"] = javaHome;
            }

            // Build arguments using strategies
            var allArguments = await BuildAllArgumentsAsync(javaVersion, serverJarPath, options);
            
            foreach (var arg in allArguments)
            {
                processStartInfo.ArgumentList.Add(arg);
            }

            _logger.LogInformation("Created process start info for Java {JavaVersion} with {ArgumentCount} arguments", 
                javaVersion, processStartInfo.ArgumentList.Count);
            
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Full command line: {FileName} {Arguments}", 
                    processStartInfo.FileName, 
                    string.Join(" ", processStartInfo.ArgumentList));
            }

            return processStartInfo;
        }

        public ProcessStartInfo CreateJavaVersionDetectionProcess(string javaExecutablePath)
        {
            if (string.IsNullOrWhiteSpace(javaExecutablePath))
                throw new ArgumentException("Java executable path cannot be null or empty", nameof(javaExecutablePath));

            return new ProcessStartInfo(javaExecutablePath)
            {
                Arguments = "-version",
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
        }

        private Task<string[]> BuildAllArgumentsAsync(int javaVersion, string serverJarPath, MinecraftServerOptions options)
        {
            var argumentList = new System.Collections.Generic.List<string>();

            // Get Java strategy and build Java-specific arguments
            var javaStrategy = _javaStrategyFactory.GetStrategy(javaVersion);
            
            // Memory arguments
            argumentList.AddRange(javaStrategy.GetMemoryArguments(options));
            
            // Garbage collection arguments
            argumentList.AddRange(javaStrategy.GetGarbageCollectionArguments());
            
            // Additional Java arguments
            argumentList.AddRange(javaStrategy.GetAdditionalArguments());

            // JAR execution
            argumentList.Add("-jar");
            argumentList.Add(serverJarPath);

            // Get Minecraft strategy and build Minecraft-specific arguments
            var minecraftStrategy = _minecraftStrategyFactory.GetStrategy(options.MinecraftVersion);
            argumentList.AddRange(minecraftStrategy.GetMinecraftArguments(options));

            return Task.FromResult(argumentList.ToArray());
        }
    }
}