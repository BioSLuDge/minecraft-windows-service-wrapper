using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace minecraft_windows_service_wrapper.Services
{
    public class ArgumentsBuilderService : IArgumentsBuilderService
    {
        private readonly ILogger<ArgumentsBuilderService> _logger;

        public ArgumentsBuilderService(ILogger<ArgumentsBuilderService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<string> BuildJavaArguments(int javaVersion, string serverJarPath, CommandLineOptions options)
        {
            if (string.IsNullOrWhiteSpace(serverJarPath))
                throw new ArgumentException("Server JAR path cannot be null or empty", nameof(serverJarPath));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var args = new List<string>();

            // Memory settings
            args.AddRange(GetMemoryArguments(javaVersion));

            // Garbage collection and performance arguments
            args.AddRange(GetGarbageCollectionArguments(javaVersion));

            // JAR execution
            args.Add("-jar");
            args.Add(serverJarPath);

            _logger.LogDebug("Built {Count} Java arguments for version {Version}", args.Count, javaVersion);
            return args;
        }

        public IEnumerable<string> BuildMinecraftArguments(CommandLineOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var args = new List<string>();

            // Add GUI and port arguments based on Minecraft version
            if (options.MinecraftVersion.Minor == 12)
            {
                args.Add("nogui");
                args.Add("--port");
                args.Add(options.Port.ToString());
            }
            else if (options.MinecraftVersion.Minor >= 16)
            {
                args.Add("--nogui");
                args.Add("--port");
                args.Add(options.Port.ToString());
            }
            else
            {
                throw new NotSupportedException($"Minecraft version {options.MinecraftVersion} is not supported");
            }

            _logger.LogDebug("Built {Count} Minecraft arguments for version {Version}", args.Count, options.MinecraftVersion);
            return args;
        }

        public IEnumerable<string> BuildAllArguments(int javaVersion, string serverJarPath, CommandLineOptions options)
        {
            var javaArgs = BuildJavaArguments(javaVersion, serverJarPath, options);
            var minecraftArgs = BuildMinecraftArguments(options);
            
            return javaArgs.Concat(minecraftArgs);
        }

        private IEnumerable<string> GetMemoryArguments(int javaVersion)
        {
            return javaVersion switch
            {
                8 => new[] { "-Xmx8G", "-Xms3G" }, // Pixelmon recommends minimum 3G
                11 or 17 or 21 => new[] { "-Xmx8G", "-Xms2G" },
                _ => throw new NotSupportedException($"Java version {javaVersion} is not supported")
            };
        }

        private IEnumerable<string> GetGarbageCollectionArguments(int javaVersion)
        {
            return javaVersion switch
            {
                8 => GetJava8GcArguments(),
                11 => GetJava11GcArguments(),
                17 or 21 => GetModernJavaGcArguments(),
                _ => throw new NotSupportedException($"Java version {javaVersion} is not supported")
            };
        }

        private IEnumerable<string> GetJava8GcArguments()
        {
            // Pixelmon-optimized flags for Java 8
            return new[]
            {
                "-XX:+UseG1GC",
                "-XX:+UnlockExperimentalVMOptions",
                "-XX:MaxGCPauseMillis=100",
                "-XX:+DisableExplicitGC",
                "-XX:TargetSurvivorRatio=90",
                "-XX:G1NewSizePercent=50",
                "-XX:G1MaxNewSizePercent=80",
                "-XX:G1MixedGCLiveThresholdPercent=50",
                "-XX:+AlwaysPreTouch"
            };
        }

        private IEnumerable<string> GetJava11GcArguments()
        {
            // Java 11 optimized flags
            return new[]
            {
                "-XX:+UseG1GC",
                "-XX:+UnlockExperimentalVMOptions",
                "-XX:MaxGCPauseMillis=100",
                "-XX:+DisableExplicitGC",
                "-XX:TargetSurvivorRatio=90",
                "-XX:G1NewSizePercent=50",
                "-XX:G1MaxNewSizePercent=80",
                "-XX:G1MixedGCLiveThresholdPercent=50",
                "-XX:+AlwaysPreTouch"
            };
        }

        private IEnumerable<string> GetModernJavaGcArguments()
        {
            // Aikar's flags for Java 17+ (modern Minecraft servers)
            return new[]
            {
                "-XX:+UseG1GC",
                "-XX:+ParallelRefProcEnabled",
                "-XX:MaxGCPauseMillis=200",
                "-XX:+UnlockExperimentalVMOptions",
                "-XX:+DisableExplicitGC",
                "-XX:+AlwaysPreTouch",
                "-XX:G1NewSizePercent=30",
                "-XX:G1MaxNewSizePercent=40",
                "-XX:G1HeapRegionSize=8M",
                "-XX:G1ReservePercent=20",
                "-XX:G1MixedGCCountTarget=4",
                "-XX:InitiatingHeapOccupancyPercent=15",
                "-XX:G1MixedGCLiveThresholdPercent=90",
                "-XX:G1RSetUpdatingPauseTimePercent=5",
                "-XX:SurvivorRatio=32",
                "-XX:+PerfDisableSharedMem",
                "-XX:MaxTenuringThreshold=1",
                "-Dusing.aikars.flags=https://mcflags.emc.gs",
                "-Daikars.new.flags=true"
            };
        }
    }
}