using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;

namespace MinecraftServer.Tests
{
    public class ConfigurationValidatorServiceTests
    {
        private static MinecraftServerOptions CreateValidOptions(string tempDir)
        {
            var jarPath = Path.Combine(tempDir, "server.jar");
            File.WriteAllText(jarPath, string.Empty);
            return new MinecraftServerOptions
            {
                ServerDirectory = tempDir,
                JarFileName = "server.jar",
                MinecraftVersion = new Version(1, 16),
                MaxMemoryMB = 4096,
                MinMemoryMB = 1024,
                GracefulShutdownTimeout = TimeSpan.FromMinutes(1)
            };
        }

        [Fact]
        public void ValidateConfiguration_NullOptions_ReturnsError()
        {
            var service = new ConfigurationValidatorService(Mock.Of<ILogger<ConfigurationValidatorService>>());
            var result = service.ValidateConfiguration(null);
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.Contains("Configuration options cannot be null", result.ErrorMessage);
        }

        [Fact]
        public void ValidateConfiguration_InvalidMemorySettings_ReturnsError()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                var options = CreateValidOptions(tempDir);
                options.MinMemoryMB = 4096;
                options.MaxMemoryMB = 1024;

                var service = new ConfigurationValidatorService(Mock.Of<ILogger<ConfigurationValidatorService>>());
                var result = service.ValidateConfiguration(options);

                Assert.NotEqual(ValidationResult.Success, result);
                Assert.Contains("Minimum memory", result.ErrorMessage);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ValidateConfiguration_ValidOptions_ReturnsSuccess()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                var options = CreateValidOptions(tempDir);

                var service = new ConfigurationValidatorService(Mock.Of<ILogger<ConfigurationValidatorService>>());
                var result = service.ValidateConfiguration(options);

                Assert.Equal(ValidationResult.Success, result);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
