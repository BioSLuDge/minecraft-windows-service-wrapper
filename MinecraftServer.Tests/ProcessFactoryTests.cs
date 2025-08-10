using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using minecraft_windows_service_wrapper.Factories;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;
using minecraft_windows_service_wrapper.Strategies.Java;
using minecraft_windows_service_wrapper.Strategies.Minecraft;

namespace MinecraftServer.Tests
{
    public class ProcessFactoryTests
    {
        [Fact]
        public async Task CreateMinecraftServerProcessAsync_OverridesExistingJavaHome()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ProcessFactory>>();
            var mockJavaVersionService = new Mock<IJavaVersionService>();
            var mockJavaStrategyFactory = new Mock<IJavaVersionStrategyFactory>();
            var mockMinecraftStrategyFactory = new Mock<IMinecraftVersionStrategyFactory>();

            mockJavaVersionService.Setup(s => s.GetJavaHomeAsync())
                .ReturnsAsync("/existing/java");
            mockJavaVersionService.Setup(s => s.GetJavaExecutablePathAsync(It.IsAny<string>()))
                .ReturnsAsync("/path/to/java");
            mockJavaVersionService.Setup(s => s.GetJavaVersionAsync(It.IsAny<string>()))
                .ReturnsAsync(17);

            var javaStrategy = new Mock<IJavaVersionStrategy>();
            javaStrategy.Setup(s => s.GetMemoryArguments(It.IsAny<MinecraftServerOptions>()))
                .Returns(Array.Empty<string>());
            javaStrategy.Setup(s => s.GetGarbageCollectionArguments())
                .Returns(Array.Empty<string>());
            javaStrategy.Setup(s => s.GetAdditionalArguments())
                .Returns(Array.Empty<string>());
            mockJavaStrategyFactory.Setup(s => s.GetStrategy(It.IsAny<int>()))
                .Returns(javaStrategy.Object);

            var minecraftStrategy = new Mock<IMinecraftVersionStrategy>();
            minecraftStrategy.Setup(s => s.GetMinecraftArguments(It.IsAny<MinecraftServerOptions>()))
                .Returns(Array.Empty<string>());
            mockMinecraftStrategyFactory.Setup(s => s.GetStrategy(It.IsAny<Version>()))
                .Returns(minecraftStrategy.Object);

            var factory = new ProcessFactory(
                mockLogger.Object,
                mockJavaVersionService.Object,
                mockJavaStrategyFactory.Object,
                mockMinecraftStrategyFactory.Object);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var jarPath = Path.Combine(tempDir, "server.jar");
            File.WriteAllText(jarPath, string.Empty);

            var options = new MinecraftServerOptions
            {
                ServerDirectory = tempDir,
                JarFileName = "server.jar",
                JavaHome = "/custom/java",
                MinecraftVersion = new Version(1, 16, 5)
            };

            Environment.SetEnvironmentVariable("JAVA_HOME", "/preexisting");

            try
            {
                // Act
                var psi = await factory.CreateMinecraftServerProcessAsync(options);

                // Assert
                Assert.Equal("/custom/java", psi.Environment["JAVA_HOME"]);
            }
            finally
            {
                Environment.SetEnvironmentVariable("JAVA_HOME", null);
                File.Delete(jarPath);
                Directory.Delete(tempDir);
            }
        }

        [Fact]
        public void CreateJavaVersionDetectionProcess_SetsExpectedProperties()
        {
            // Arrange
            var factory = new ProcessFactory(
                Mock.Of<ILogger<ProcessFactory>>(),
                Mock.Of<IJavaVersionService>(),
                Mock.Of<IJavaVersionStrategyFactory>(),
                Mock.Of<IMinecraftVersionStrategyFactory>());

            // Act
            var psi = factory.CreateJavaVersionDetectionProcess("java.exe");

            // Assert
            Assert.Equal("java.exe", psi.FileName);
            Assert.Equal("-version", psi.Arguments);
            Assert.True(psi.RedirectStandardInput);
            Assert.True(psi.RedirectStandardOutput);
            Assert.True(psi.RedirectStandardError);
            Assert.False(psi.UseShellExecute);
            Assert.Equal(ProcessWindowStyle.Hidden, psi.WindowStyle);
        }
    }
}
