using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace minecraft_windows_service_wrapper.Strategies.Minecraft
{
    public class MinecraftVersionStrategyFactory : IMinecraftVersionStrategyFactory
    {
        private readonly ILogger<MinecraftVersionStrategyFactory> _logger;
        private readonly List<IMinecraftVersionStrategy> _strategies;

        public MinecraftVersionStrategyFactory(ILogger<MinecraftVersionStrategyFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _strategies = new List<IMinecraftVersionStrategy>
            {
                new Minecraft12Strategy(),
                new ModernMinecraftStrategy()
            };
        }

        public IMinecraftVersionStrategy GetStrategy(Version minecraftVersion)
        {
            if (minecraftVersion == null)
                throw new ArgumentNullException(nameof(minecraftVersion));

            var strategy = _strategies.FirstOrDefault(s => s.SupportsVersion(minecraftVersion));
            
            if (strategy == null)
            {
                _logger.LogError("No strategy found for Minecraft version {Version}", minecraftVersion);
                throw new NotSupportedException($"Minecraft version {minecraftVersion} is not supported");
            }

            _logger.LogDebug("Using strategy for Minecraft version {Version}", minecraftVersion);
            return strategy;
        }

        public bool IsVersionSupported(Version minecraftVersion)
        {
            if (minecraftVersion == null)
                return false;

            return _strategies.Any(s => s.SupportsVersion(minecraftVersion));
        }
    }
}