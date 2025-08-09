using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace minecraft_windows_service_wrapper.Services
{
    public class StreamRelayService : IStreamRelayService, IDisposable
    {
        private readonly ILogger<StreamRelayService> _logger;
        private Task _outputReaderTask;
        private Task _errorReaderTask;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        public event EventHandler<string> OutputReceived;
        public event EventHandler<string> ErrorReceived;

        public StreamRelayService(ILogger<StreamRelayService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartRelayingAsync(Process process, CancellationToken cancellationToken = default)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            if (_outputReaderTask != null || _errorReaderTask != null)
                throw new InvalidOperationException("Stream relaying is already active. Stop current relaying before starting new one.");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                _logger.LogDebug("Starting stream relay for process {ProcessId}", process.Id);

                _outputReaderTask = StartStreamReaderAsync(
                    process.StandardOutput, 
                    false, 
                    _cancellationTokenSource.Token);

                _errorReaderTask = StartStreamReaderAsync(
                    process.StandardError, 
                    true, 
                    _cancellationTokenSource.Token);

                _logger.LogDebug("Stream relay started for process {ProcessId}", process.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start stream relay for process {ProcessId}", process.Id);
                await StopRelayingAsync();
                throw;
            }
        }

        public async Task StopRelayingAsync()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogDebug("Stopping stream relay");
                _cancellationTokenSource.Cancel();
            }

            try
            {
                if (_outputReaderTask != null)
                {
                    await _outputReaderTask.ConfigureAwait(false);
                    _outputReaderTask = null;
                }

                if (_errorReaderTask != null)
                {
                    await _errorReaderTask.ConfigureAwait(false);
                    _errorReaderTask = null;
                }

                _logger.LogDebug("Stream relay stopped");
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                _logger.LogDebug("Stream relay stopped due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while stopping stream relay");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task StartStreamReaderAsync(StreamReader reader, bool isError, CancellationToken cancellationToken)
        {
            var streamType = isError ? "Error" : "Output";
            
            try
            {
                _logger.LogDebug("Starting {StreamType} stream reader", streamType);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    
                    if (line == null)
                    {
                        _logger.LogDebug("{StreamType} stream ended", streamType);
                        break;
                    }

                    // Log the message
                    if (isError)
                    {
                        _logger.LogError("[Minecraft Error] {Message}", line);
                    }
                    else
                    {
                        _logger.LogInformation("[Minecraft] {Message}", line);
                    }

                    // Raise events for external handling
                    try
                    {
                        if (isError)
                            ErrorReceived?.Invoke(this, line);
                        else
                            OutputReceived?.Invoke(this, line);
                    }
                    catch (Exception eventEx)
                    {
                        _logger.LogWarning(eventEx, "Error in event handler for {StreamType} stream", streamType);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("{StreamType} stream reader cancelled", streamType);
            }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug("{StreamType} stream reader stopped - stream disposed", streamType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {StreamType} stream reader", streamType);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Cancel any ongoing operations
                _cancellationTokenSource?.Cancel();

                // Wait for tasks to complete (with timeout)
                var timeout = TimeSpan.FromSeconds(5);
                var waitTasks = new Task[] { _outputReaderTask, _errorReaderTask }
                    .Where(t => t != null)
                    .ToArray();

                if (waitTasks.Length > 0)
                {
                    if (!Task.WaitAll(waitTasks, timeout))
                    {
                        _logger.LogWarning("Stream relay tasks did not complete within timeout during disposal");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during StreamRelayService disposal");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _outputReaderTask = null;
                _errorReaderTask = null;
                _disposed = true;
            }
        }
    }
}