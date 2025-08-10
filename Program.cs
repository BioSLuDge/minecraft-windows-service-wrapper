using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using minecraft_windows_service_wrapper.Factories;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using minecraft_windows_service_wrapper.Strategies.Java;
using minecraft_windows_service_wrapper.Strategies.Minecraft;

namespace minecraft_windows_service_wrapper
{
    internal static class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            var options = SettingsService.Load();

            if (Environment.UserInteractive)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Create JavaVersionService for the UI with proper error handling
                var logger = NullLogger<JavaVersionService>.Instance;
                var javaVersionService = new JavaVersionService(logger);
                
                Application.Run(new MainForm(javaVersionService));
            }
            else
            {
                await CreateHostBuilder(options).Build().RunAsync();
            }
        }

        public static IHostBuilder CreateHostBuilder(MinecraftServerOptions opts) =>
            Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                    logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Information))
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<MinecraftServerOptions>(config =>
                    {
                        config.ServerDirectory = opts.ServerDirectory;
                        config.MinecraftVersion = opts.MinecraftVersion;
                        config.Port = opts.Port;
                        config.JavaHome = opts.JavaHome;
                        config.JarFileName = opts.JarFileName;
                        config.MaxMemoryMB = opts.MaxMemoryMB;
                        config.MinMemoryMB = opts.MinMemoryMB;
                        config.EnableDetailedLogging = opts.EnableDetailedLogging;
                        config.GracefulShutdownTimeout = opts.GracefulShutdownTimeout;
                    });

                    services.AddSingleton<IJavaVersionService, JavaVersionService>();
                    services.AddSingleton<IProcessManagerService, ProcessManagerService>();
                    services.AddSingleton<IArgumentsBuilderService, ArgumentsBuilderService>();
                    services.AddSingleton<IStreamRelayService, StreamRelayService>();
                    services.AddSingleton<IConfigurationValidatorService, ConfigurationValidatorService>();

                    services.AddSingleton<IJavaVersionStrategyFactory, JavaVersionStrategyFactory>();
                    services.AddSingleton<IMinecraftVersionStrategyFactory, MinecraftVersionStrategyFactory>();

                    services.AddSingleton<IProcessFactory, ProcessFactory>();

                    services.AddHostedService<MinecraftServer>()
                        .Configure<EventLogSettings>(config =>
                        {
                            config.LogName = "Application";
                            config.SourceName = "Minecraft Windows Service Wrapper";
                        });
                })
                .UseWindowsService();
    }
}
