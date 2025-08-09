using System.Collections.Generic;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Strategies.Java
{
    public class Java8Strategy : IJavaVersionStrategy
    {
        public int SupportedJavaVersion => 8;

        public IEnumerable<string> GetMemoryArguments(MinecraftServerOptions options)
        {
            yield return $"-Xmx{options.MaxMemoryMB}M";
            yield return $"-Xms{options.MinMemoryMB}M";
        }

        public IEnumerable<string> GetGarbageCollectionArguments()
        {
            // Pixelmon-optimized flags for Java 8
            yield return "-XX:+UseG1GC";
            yield return "-XX:+UnlockExperimentalVMOptions";
            yield return "-XX:MaxGCPauseMillis=100";
            yield return "-XX:+DisableExplicitGC";
            yield return "-XX:TargetSurvivorRatio=90";
            yield return "-XX:G1NewSizePercent=50";
            yield return "-XX:G1MaxNewSizePercent=80";
            yield return "-XX:G1MixedGCLiveThresholdPercent=50";
            yield return "-XX:+AlwaysPreTouch";
        }

        public IEnumerable<string> GetAdditionalArguments()
        {
            // No additional arguments for Java 8
            yield break;
        }
    }
}