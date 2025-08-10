using System;
using System.Collections.Generic;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Strategies.Minecraft
{
    public class ModernMinecraftStrategy : IMinecraftVersionStrategy
    {
        public Version SupportedVersion => new Version(1, 16);

        public bool SupportsVersion(Version version)
        {
            return version?.Major == 1 && version.Minor >= 16;
        }

        public IEnumerable<string> GetMinecraftArguments(MinecraftServerOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Minecraft 1.16+ uses '--nogui' with dashes
            yield return "--nogui";

            // Port handling: only specify when non-negative
            if (options.Port >= 0)
            {
                yield return "--port";
                yield return options.Port.ToString();
            }
        }
    }
}