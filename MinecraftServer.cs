using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using minecraft_windows_service_wrapper.Factories;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;

namespace minecraft_windows_service_wrapper
{
    public class MinecraftServer : BackgroundService
    {
        private readonly ILogger<MinecraftServer> _logger;
        private readonly MinecraftServerOptions _options;
        private readonly IProcessFactory _processFactory;
        private readonly IProcessManagerService _processManager;
        private readonly IStreamRelayService _streamRelay;
        private readonly IConfigurationValidatorService _configValidator;

        public MinecraftServer(
            ILogger<MinecraftServer> logger,
            IOptions<MinecraftServerOptions> options,
            IProcessFactory processFactory,
            IProcessManagerService processManager,
            IStreamRelayService streamRelay,
            IConfigurationValidatorService configValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _processFactory = processFactory ?? throw new ArgumentNullException(nameof(processFactory));
            _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
            _streamRelay = streamRelay ?? throw new ArgumentNullException(nameof(streamRelay));
            _configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting Minecraft Windows Service Wrapper");
                
                // Validate configuration
                var validationResult = _configValidator.ValidateConfiguration(_options);
                if (validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
                {
                    throw new InvalidOperationException($"Configuration validation failed: {validationResult.ErrorMessage}");
                }

                _logger.LogInformation("Configuration validated successfully");
                _logger.LogInformation("Server directory: {ServerDirectory}", _options.ServerDirectory);
                _logger.LogInformation("JAR file: {JarFile}", _options.JarFileName);
                _logger.LogInformation("Minecraft version: {Version}", _options.MinecraftVersion);
                _logger.LogInformation("Port: {Port}", _options.Port == -1 ? "25565 (default)" : _options.Port.ToString());

                // Create process start info using factory
                var processStartInfo = await _processFactory.CreateMinecraftServerProcessAsync(_options);
                
                // Start the Minecraft server process
                var process = await _processManager.StartProcessAsync(processStartInfo, stoppingToken);
                _logger.LogInformation("Minecraft server process started with PID: {ProcessId}", process.Id);

                // Start stream relaying
                await _streamRelay.StartRelayingAsync(process, stoppingToken);
                _logger.LogInformation("Stream relay started");

                // Monitor the process and wait for cancellation
                await MonitorProcessAsync(process, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Minecraft server service is stopping due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Minecraft server service");
                throw;
            }
        }

        private async Task MonitorProcessAsync(System.Diagnostics.Process process, CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Check if the process is still running
                    var isRunning = await _processManager.IsProcessRunningAsync(process);
                    if (!isRunning)
                    {
                        _logger.LogWarning("Minecraft server process has exited unexpectedly");
                        break;
                    }

                    // Wait a bit before checking again
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Process monitoring cancelled");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Minecraft server service");

            try
            {
                // Stop stream relaying first
                await _streamRelay.StopRelayingAsync();
                _logger.LogInformation("Stream relay stopped");

                // Get the current process if any
                // Note: This is a simplified approach. In a real implementation,
                // we might need to track the process reference differently
                // For now; the ProcessManagerService handles graceful shutdown
                // when it's disposed

                _logger.LogInformation("Minecraft server service stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during service shutdown");
            }
            finally
            {
                await base.StopAsync(cancellationToken);
            }
        }

        public override void Dispose()
        {
            try
            {
                _streamRelay?.Dispose();
                _processManager?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during disposal");
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}