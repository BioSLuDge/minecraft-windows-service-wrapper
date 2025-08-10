using System;
using System.Linq;
using Xunit;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Strategies.Minecraft;

namespace MinecraftServer.Tests
{
    public class MinecraftVersionStrategyTests
    {
        [Fact]
        public void Minecraft12Strategy_SkipsPortWhenNegative()
        {
            var strategy = new Minecraft12Strategy();
            var options = new MinecraftServerOptions
            {
                MinecraftVersion = new Version(1, 12),
                Port = -1
            };

            var args = strategy.GetMinecraftArguments(options).ToList();
            Assert.Contains("nogui", args);
            Assert.DoesNotContain("--port", args);
        }

        [Fact]
        public void ModernMinecraftStrategy_SkipsPortWhenNegative()
        {
            var strategy = new ModernMinecraftStrategy();
            var options = new MinecraftServerOptions
            {
                MinecraftVersion = new Version(1, 16),
                Port = -1
            };

            var args = strategy.GetMinecraftArguments(options).ToList();
            Assert.Contains("--nogui", args);
            Assert.DoesNotContain("--port", args);
        }
    }
}
