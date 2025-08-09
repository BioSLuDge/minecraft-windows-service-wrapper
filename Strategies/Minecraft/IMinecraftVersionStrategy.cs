using System;
using System.Collections.Generic;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Strategies.Minecraft
{
    public interface IMinecraftVersionStrategy
    {
        Version SupportedVersion { get; }
        bool SupportsVersion(Version version);
        IEnumerable<string> GetMinecraftArguments(MinecraftServerOptions options);
    }
}