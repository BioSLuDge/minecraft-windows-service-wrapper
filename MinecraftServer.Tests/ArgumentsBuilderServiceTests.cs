using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using minecraft_windows_service_wrapper.Services;
using minecraft_windows_service_wrapper;

namespace MinecraftServer.Tests
{
    public class ArgumentsBuilderServiceTests
    {
        private readonly Mock<ILogger<ArgumentsBuilderService>> _mockLogger;
        private readonly ArgumentsBuilderService _service;
        private readonly CommandLineOptions _defaultOptions;

        public ArgumentsBuilderServiceTests()
        {
            _mockLogger = new Mock<ILogger<ArgumentsBuilderService>>();
            _service = new ArgumentsBuilderService(_mockLogger.Object);
            _defaultOptions = new CommandLineOptions
            {
                ServerDirectory = @"C:\test\server",
                MinecraftVersion = new Version(1, 16, 5),
                Port = 25565,
                JarFileName = "server.jar"
            };
        }

        [Theory]
        [InlineData(8)]
        [InlineData(11)]
        [InlineData(17)]
        [InlineData(21)]
        public void BuildJavaArguments_WhenValidJavaVersion_ReturnsCorrectArguments(int javaVersion)
        {
            // Arrange
            var serverJarPath = @"C:\test\server\server.jar";

            // Act
            var result = _service.BuildJavaArguments(javaVersion, serverJarPath, _defaultOptions);
            var args = result.ToList();

            // Assert
            Assert.Contains("-Xmx8G", args);
            Assert.Contains("-jar", args);
            Assert.Contains(serverJarPath, args);
            Assert.True(args.Count > 5);
        }

        [Fact]
        public void BuildMinecraftArguments_WhenMinecraft16Plus_UsesCorrectFormat()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                MinecraftVersion = new Version(1, 16, 5),
                Port = 25565
            };

            // Act
            var result = _service.BuildMinecraftArguments(options);
            var args = result.ToList();

            // Assert
            Assert.Contains("--nogui", args);
            Assert.Contains("--port", args);
            Assert.Contains("25565", args);
        }
    }
}