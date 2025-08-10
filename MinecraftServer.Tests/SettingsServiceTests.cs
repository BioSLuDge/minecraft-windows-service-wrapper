using System;
using System.IO;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;
using Xunit;

namespace MinecraftServer.Tests
{
    public class SettingsServiceTests
    {
        [Fact]
        public void Load_WithMalformedJson_ReturnsDefaultOptions()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var previousAppData = Environment.GetEnvironmentVariable("APPDATA");
            var previousXdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

            try
            {
                Environment.SetEnvironmentVariable("APPDATA", tempDir);
                Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", tempDir);

                var configDir = Path.Combine(tempDir, "MinecraftServiceWrapper");
                Directory.CreateDirectory(configDir);
                var configPath = Path.Combine(configDir, "settings.json");
                File.WriteAllText(configPath, "{ invalid json");

                var options = SettingsService.Load();
                var defaults = MinecraftServerOptions.CreateDefault();

                Assert.Equal(defaults.Port, options.Port);
                Assert.Equal(defaults.JarFileName, options.JarFileName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("APPDATA", previousAppData);
                Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", previousXdgConfigHome);
                Directory.Delete(tempDir, true);
            }
        }
    }
}
