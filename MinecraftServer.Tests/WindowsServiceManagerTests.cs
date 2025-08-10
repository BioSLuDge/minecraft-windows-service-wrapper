using System;
using System.Diagnostics;
using Moq;
using Xunit;
using minecraft_windows_service_wrapper.Services;

namespace MinecraftServer.Tests
{
    public class WindowsServiceManagerTests
    {
        [Fact]
        public void Install_Success_DoesNotThrow()
        {
            var mockRunner = new Mock<IProcessRunner>();
            mockRunner.Setup(r => r.Run(It.IsAny<ProcessStartInfo>()))
                .Returns(new ProcessResult { ExitCode = 0, StandardOutput = "ok" });

            WindowsServiceManager.Install("svc", "path", mockRunner.Object);
        }

        [Fact]
        public void Install_Failure_Throws()
        {
            var mockRunner = new Mock<IProcessRunner>();
            mockRunner.Setup(r => r.Run(It.IsAny<ProcessStartInfo>()))
                .Returns(new ProcessResult { ExitCode = 1, StandardError = "error" });

            Assert.Throws<InvalidOperationException>(() =>
                WindowsServiceManager.Install("svc", "path", mockRunner.Object));
        }

        [Fact]
        public void Remove_Success_DoesNotThrow()
        {
            var mockRunner = new Mock<IProcessRunner>();
            mockRunner.Setup(r => r.Run(It.IsAny<ProcessStartInfo>()))
                .Returns(new ProcessResult { ExitCode = 0 });

            WindowsServiceManager.Remove("svc", mockRunner.Object);
        }

        [Fact]
        public void Remove_Failure_Throws()
        {
            var mockRunner = new Mock<IProcessRunner>();
            mockRunner.Setup(r => r.Run(It.IsAny<ProcessStartInfo>()))
                .Returns(new ProcessResult { ExitCode = 2, StandardError = "not found" });

            Assert.Throws<InvalidOperationException>(() =>
                WindowsServiceManager.Remove("svc", mockRunner.Object));
        }
    }
}
