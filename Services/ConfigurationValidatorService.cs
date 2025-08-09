using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Logging;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Services
{
    public class ConfigurationValidatorService : IConfigurationValidatorService
    {
        private readonly ILogger<ConfigurationValidatorService> _logger;

        public ConfigurationValidatorService(ILogger<ConfigurationValidatorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ValidationResult ValidateConfiguration(MinecraftServerOptions options)
        {
            if (options == null)
                return new ValidationResult("Configuration options cannot be null");

            var validationResults = ValidateAllProperties(options).ToList();

            if (validationResults.Any())
            {
                var errorMessages = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                _logger.LogError("Configuration validation failed: {Errors}", errorMessages);
                return new ValidationResult($"Configuration validation failed: {errorMessages}");
            }

            _logger.LogDebug("Configuration validation passed");
            return ValidationResult.Success;
        }

        public IEnumerable<ValidationResult> ValidateAllProperties(MinecraftServerOptions options)
        {
            if (options == null)
            {
                yield return new ValidationResult("Configuration options cannot be null");
                yield break;
            }

            var validationContext = new ValidationContext(options, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();

            // Validate all data annotations
            Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true);

            foreach (var result in validationResults)
            {
                yield return result;
            }

            // Additional cross-property validation
            foreach (var result in ValidateCrossProperties(options))
            {
                yield return result;
            }
        }

        public bool IsValid(MinecraftServerOptions options)
        {
            return !ValidateAllProperties(options).Any();
        }

        private IEnumerable<ValidationResult> ValidateCrossProperties(MinecraftServerOptions options)
        {
            // Validate memory settings
            if (options.MinMemoryMB > options.MaxMemoryMB)
            {
                yield return new ValidationResult(
                    $"Minimum memory ({options.MinMemoryMB}MB) cannot be greater than maximum memory ({options.MaxMemoryMB}MB)",
                    new[] { nameof(options.MinMemoryMB), nameof(options.MaxMemoryMB) });
            }

            // Validate timeout settings
            if (options.GracefulShutdownTimeout <= TimeSpan.Zero)
            {
                yield return new ValidationResult(
                    "Graceful shutdown timeout must be greater than zero",
                    new[] { nameof(options.GracefulShutdownTimeout) });
            }

            if (options.GracefulShutdownTimeout > TimeSpan.FromMinutes(10))
            {
                yield return new ValidationResult(
                    "Graceful shutdown timeout should not exceed 10 minutes",
                    new[] { nameof(options.GracefulShutdownTimeout) });
            }
        }
    }
}