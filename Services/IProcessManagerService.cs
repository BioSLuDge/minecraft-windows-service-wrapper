using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace minecraft_windows_service_wrapper.Services
{
    public interface IProcessManagerService
    {
        Task<Process> StartProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default);
        Task StopProcessGracefullyAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken = default);
        Task<bool> IsProcessRunningAsync(Process process);
        Task SendCommandAsync(Process process, string command);
        void Dispose();
    }
}