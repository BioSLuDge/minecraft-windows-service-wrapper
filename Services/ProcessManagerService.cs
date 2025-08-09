using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace minecraft_windows_service_wrapper.Services
{
    public class ProcessManagerService : IProcessManagerService, IDisposable
    {
        private readonly ILogger<ProcessManagerService> _logger;
        private Process _currentProcess;
        private bool _disposed;

        public ProcessManagerService(ILogger<ProcessManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Process> StartProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default)
        {
            if (startInfo == null)
                throw new ArgumentNullException(nameof(startInfo));

            if (_currentProcess != null && !_currentProcess.HasExited)
                throw new InvalidOperationException("A process is already running. Stop the current process before starting a new one.");

            try
            {
                _logger.LogInformation("Starting process: {FileName} {Arguments}", 
                    startInfo.FileName, 
                    string.Join(" ", startInfo.ArgumentList));

                var process = Process.Start(startInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start process - Process.Start returned null");

                _currentProcess = process;
                
                _logger.LogInformation("Process started with PID: {ProcessId}", process.Id);
                return process;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start process: {FileName}", startInfo.FileName);
                throw;
            }
        }

        public async Task StopProcessGracefullyAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            if (process.HasExited)
            {
                _logger.LogInformation("Process {ProcessId} has already exited", process.Id);
                return;
            }

            try
            {
                _logger.LogInformation("Initiating graceful shutdown for process {ProcessId}", process.Id);
                
                // Send save-all command
                await SendCommandAsync(process, "save-all");
                await Task.Delay(1000, cancellationToken); // Give time for save to complete
                
                // Send stop command
                await SendCommandAsync(process, "stop");
                
                _logger.LogInformation("Waiting for process {ProcessId} to exit gracefully (timeout: {Timeout})", 
                    process.Id, timeout);

                // Wait for graceful shutdown with timeout
                var completed = await WaitForExitAsync(process, timeout, cancellationToken);
                
                if (completed)
                {
                    _logger.LogInformation("Process {ProcessId} exited gracefully with code {ExitCode}", 
                        process.Id, process.ExitCode);
                }
                else
                {
                    _logger.LogWarning("Process {ProcessId} did not exit within timeout, forcing termination", process.Id);
                    ForceKillProcess(process);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during graceful shutdown of process {ProcessId}", process.Id);
                ForceKillProcess(process);
                throw;
            }
        }

        public Task<bool> IsProcessRunningAsync(Process process)
        {
            if (process == null)
                return Task.FromResult(false);

            try
            {
                return Task.FromResult(!process.HasExited);
            }
            catch (InvalidOperationException)
            {
                // Process was not started by this component or has been disposed
                return Task.FromResult(false);
            }
        }

        public async Task SendCommandAsync(Process process, string command)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            if (process.HasExited)
                throw new InvalidOperationException($"Cannot send command to exited process {process.Id}");

            try
            {
                _logger.LogDebug("Sending command to process {ProcessId}: {Command}", process.Id, command);
                await process.StandardInput.WriteLineAsync(command);
                await process.StandardInput.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command '{Command}' to process {ProcessId}", command, process.Id);
                throw;
            }
        }

        private async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(combinedCts.Token);
                return true;
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                return false; // Timeout occurred
            }
            catch (OperationCanceledException)
            {
                throw; // Cancellation requested by caller
            }
        }

        private void ForceKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    _logger.LogWarning("Force killing process {ProcessId}", process.Id);
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to force kill process {ProcessId}", process.Id);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                if (_currentProcess != null && !_currentProcess.HasExited)
                {
                    _logger.LogInformation("Disposing ProcessManagerService - terminating process {ProcessId}", _currentProcess.Id);
                    ForceKillProcess(_currentProcess);
                }
                
                _currentProcess?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ProcessManagerService disposal");
            }
            finally
            {
                _currentProcess = null;
                _disposed = true;
            }
        }
    }
}