using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace minecraft_windows_service_wrapper.Options
{
    public class MinecraftServerOptions
    {
        [Required(ErrorMessage = "Server directory is required")]
        [CustomValidation(typeof(MinecraftServerOptions), nameof(ValidateServerDirectory))]
        public string ServerDirectory { get; set; }

        [Required(ErrorMessage = "Minecraft version is required")]
        [CustomValidation(typeof(MinecraftServerOptions), nameof(ValidateMinecraftVersion))]
        public Version MinecraftVersion { get; set; }

        [Range(-1, 65535, ErrorMessage = "Port must be between -1 and 65535")]
        public int Port { get; set; } = -1;

        [CustomValidation(typeof(MinecraftServerOptions), nameof(ValidateJavaHome))]
        public string JavaHome { get; set; }

        [Required(ErrorMessage = "JAR filename is required")]
        [CustomValidation(typeof(MinecraftServerOptions), nameof(ValidateJarFileName))]
        public string JarFileName { get; set; } = "server.jar";

        [Range(512, 32768, ErrorMessage = "Max memory must be between 512MB and 32GB")]
        public int MaxMemoryMB { get; set; } = 8192; // 8GB default

        [Range(256, 16384, ErrorMessage = "Min memory must be between 256MB and 16GB")]
        public int MinMemoryMB { get; set; } = 2048; // 2GB default

        public bool EnableDetailedLogging { get; set; } = false;

        public TimeSpan GracefulShutdownTimeout { get; set; } = TimeSpan.FromMinutes(2);

        // Custom validation methods
        public static ValidationResult ValidateServerDirectory(string serverDirectory, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(serverDirectory))
                return new ValidationResult("Server directory cannot be empty");

            if (!Directory.Exists(serverDirectory))
                return new ValidationResult($"Server directory does not exist: {serverDirectory}");

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateMinecraftVersion(Version minecraftVersion, ValidationContext context)
        {
            if (minecraftVersion == null)
                return new ValidationResult("Minecraft version cannot be null");

            // Support for Minecraft 1.12 and 1.16+
            if (minecraftVersion.Major != 1)
                return new ValidationResult("Only Minecraft 1.x versions are supported");

            if (minecraftVersion.Minor != 12 && minecraftVersion.Minor < 16)
                return new ValidationResult("Only Minecraft 1.12 and 1.16+ versions are supported");

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateJavaHome(string javaHome, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(javaHome))
                return ValidationResult.Success; // Optional field

            if (!Directory.Exists(javaHome))
                return new ValidationResult($"Java home directory does not exist: {javaHome}");

            var javaExe = Path.Combine(javaHome, "bin", "java.exe");
            if (!File.Exists(javaExe))
                return new ValidationResult($"Java executable not found at: {javaExe}");

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateJarFileName(string jarFileName, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(jarFileName))
                return new ValidationResult("JAR filename cannot be empty");

            if (!jarFileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                return new ValidationResult("JAR filename must end with .jar");

            var instance = context.ObjectInstance as MinecraftServerOptions;
            if (instance?.ServerDirectory != null)
            {
                var jarPath = Path.Combine(instance.ServerDirectory, jarFileName);
                if (!File.Exists(jarPath))
                    return new ValidationResult($"JAR file not found: {jarPath}");
            }

            return ValidationResult.Success;
        }

        // Helper method to convert from CommandLineOptions
        public static MinecraftServerOptions FromCommandLineOptions(CommandLineOptions cmdOptions)
        {
            if (cmdOptions == null)
                throw new ArgumentNullException(nameof(cmdOptions));

            return new MinecraftServerOptions
            {
                ServerDirectory = cmdOptions.ServerDirectory,
                MinecraftVersion = cmdOptions.MinecraftVersion,
                Port = cmdOptions.Port,
                JavaHome = cmdOptions.JavaHome,
                JarFileName = cmdOptions.JarFileName
            };
        }
    }
}