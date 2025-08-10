using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using minecraft_windows_service_wrapper.Services;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Strategies.Java;

namespace MinecraftServer.Tests
{
    public class ArgumentsBuilderServiceTests
    {
        private readonly Mock<ILogger<ArgumentsBuilderService>> _mockLogger;
        private readonly ArgumentsBuilderService _service;
        private readonly MinecraftServerOptions _defaultOptions;

        public ArgumentsBuilderServiceTests()
        {
            _mockLogger = new Mock<ILogger<ArgumentsBuilderService>>();
            _service = new ArgumentsBuilderService(_mockLogger.Object);
            _defaultOptions = new MinecraftServerOptions
            {
                ServerDirectory = @"C:\test\server",
                MinecraftVersion = new Version(1, 16, 5),
                Port = 25565,
                JarFileName = "server.jar",
                MaxMemoryMB = 4096,
                MinMemoryMB = 1024
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
            Assert.Contains("-Xmx4096M", args);
            Assert.Contains("-Xms1024M", args);
            Assert.Contains("-jar", args);
            Assert.Contains(serverJarPath, args);
            Assert.True(args.Count > 5);
        }

        [Fact]
        public void BuildMinecraftArguments_WhenMinecraft16Plus_UsesCorrectFormat()
        {
            // Arrange
            var options = new MinecraftServerOptions
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

        [Theory]
        [InlineData(8)]
        [InlineData(11)]
        [InlineData(17)]
        [InlineData(18)]
        [InlineData(19)]
        [InlineData(20)]
        [InlineData(21)]
        [InlineData(22)]
        [InlineData(23)]
        [InlineData(24)] // Future version test
        public void JavaVersionStrategyFactory_SupportsModernJavaVersions(int javaVersion)
        {
            // Arrange
            var logger = new Mock<ILogger<JavaVersionStrategyFactory>>();
            var factory = new JavaVersionStrategyFactory(logger.Object);

            // Act
            var isSupported = factory.IsVersionSupported(javaVersion);
            
            // Assert
            Assert.True(isSupported, $"Java version {javaVersion} should be supported");
            
            // Verify we can get a strategy for it
            var strategy = factory.GetStrategy(javaVersion);
            Assert.NotNull(strategy);
            Assert.Equal(javaVersion, strategy.SupportedJavaVersion);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(12)]
        [InlineData(16)]
        public void JavaVersionStrategyFactory_DoesNotSupportOldJavaVersions(int javaVersion)
        {
            // Arrange
            var logger = new Mock<ILogger<JavaVersionStrategyFactory>>();
            var factory = new JavaVersionStrategyFactory(logger.Object);

            // Act & Assert
            Assert.False(factory.IsVersionSupported(javaVersion), $"Java version {javaVersion} should not be supported");
            Assert.Throws<NotSupportedException>(() => factory.GetStrategy(javaVersion));
        }
    }
}