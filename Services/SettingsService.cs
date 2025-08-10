using System;
using System.IO;
using System.Text.Json;
using minecraft_windows_service_wrapper.Options;

namespace minecraft_windows_service_wrapper.Services
{
    public static class SettingsService
    {
        private static string GetConfigPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "MinecraftServiceWrapper");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }

        public static MinecraftServerOptions Load()
        {
            var path = GetConfigPath();
            if (!File.Exists(path))
                return MinecraftServerOptions.CreateDefault();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<MinecraftServerOptions>(json)
                    ?? MinecraftServerOptions.CreateDefault();
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Error reading settings file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Error deserializing settings file: {ex.Message}");
            }

            return MinecraftServerOptions.CreateDefault();
        }

        public static void Save(MinecraftServerOptions options)
        {
            var path = GetConfigPath();
            var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
