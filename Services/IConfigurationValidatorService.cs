using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Services
{
    public interface IConfigurationValidatorService
    {
        ValidationResult ValidateConfiguration(MinecraftServerOptions options);
        IEnumerable<ValidationResult> ValidateAllProperties(MinecraftServerOptions options);
        bool IsValid(MinecraftServerOptions options);
    }
}