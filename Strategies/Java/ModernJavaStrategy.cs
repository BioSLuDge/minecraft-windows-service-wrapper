using System;
using System.Collections.Generic;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Strategies.Java
{
    public class ModernJavaStrategy : IJavaVersionStrategy
    {
        public int SupportedJavaVersion { get; }

        public ModernJavaStrategy(int javaVersion)
        {
            if (javaVersion != 17 && javaVersion != 21)
                throw new ArgumentException($"ModernJavaStrategy only supports Java 17 and 21, got {javaVersion}");
            
            SupportedJavaVersion = javaVersion;
        }

        public IEnumerable<string> GetMemoryArguments(MinecraftServerOptions options)
        {
            yield return $"-Xmx{options.MaxMemoryMB}M";
            yield return $"-Xms{options.MinMemoryMB}M";
        }

        public IEnumerable<string> GetGarbageCollectionArguments()
        {
            // Aikar's flags for modern Java versions (17+)
            yield return "-XX:+UseG1GC";
            yield return "-XX:+ParallelRefProcEnabled";
            yield return "-XX:MaxGCPauseMillis=200";
            yield return "-XX:+UnlockExperimentalVMOptions";
            yield return "-XX:+DisableExplicitGC";
            yield return "-XX:+AlwaysPreTouch";
            yield return "-XX:G1NewSizePercent=30";
            yield return "-XX:G1MaxNewSizePercent=40";
            yield return "-XX:G1HeapRegionSize=8M";
            yield return "-XX:G1ReservePercent=20";
            yield return "-XX:G1MixedGCCountTarget=4";
            yield return "-XX:InitiatingHeapOccupancyPercent=15";
            yield return "-XX:G1MixedGCLiveThresholdPercent=90";
            yield return "-XX:G1RSetUpdatingPauseTimePercent=5";
            yield return "-XX:SurvivorRatio=32";
            yield return "-XX:+PerfDisableSharedMem";
            yield return "-XX:MaxTenuringThreshold=1";
        }

        public IEnumerable<string> GetAdditionalArguments()
        {
            // Aikar's flags metadata
            yield return "-Dusing.aikars.flags=https://mcflags.emc.gs";
            yield return "-Daikars.new.flags=true";
        }
    }
}