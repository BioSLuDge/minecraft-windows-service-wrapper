using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace minecraft_windows_service_wrapper.Services
{
    public interface IStreamRelayService
    {
        Task StartRelayingAsync(Process process, CancellationToken cancellationToken = default);
        Task StopRelayingAsync();
        event EventHandler<string> OutputReceived;
        event EventHandler<string> ErrorReceived;
        void Dispose();
    }
}