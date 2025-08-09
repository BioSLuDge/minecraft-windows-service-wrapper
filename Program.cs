using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Logging;
using CommandLine;
using System.Threading.Tasks;
using minecraft_windows_service_wrapper.Factories;
using minecraft_windows_service_wrapper.Options;
using minecraft_windows_service_wrapper.Services;
using minecraft_windows_service_wrapper.Strategies.Java;
using minecraft_windows_service_wrapper.Strategies.Minecraft;

namespace minecraft_windows_service_wrapper
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult(async (opts) =>
                {
                    await CreateHostBuilder(args, opts).Build().RunAsync();
                    return 0;
                },
                errs => Task.FromResult(-1)); // Invalid arguments
        }

        public static IHostBuilder CreateHostBuilder(string[] args, CommandLineOptions opts) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(configureLogging => 
                    configureLogging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Information))
                .ConfigureServices((hostContext, services) =>
                {
                    // Convert CommandLineOptions to MinecraftServerOptions and register
                    var serverOptions = MinecraftServerOptions.FromCommandLineOptions(opts);
                    services.Configure<MinecraftServerOptions>(config =>
                    {
                        config.ServerDirectory = serverOptions.ServerDirectory;
                        config.MinecraftVersion = serverOptions.MinecraftVersion;
                        config.Port = serverOptions.Port;
                        config.JavaHome = serverOptions.JavaHome;
                        config.JarFileName = serverOptions.JarFileName;
                        config.MaxMemoryMB = serverOptions.MaxMemoryMB;
                        config.MinMemoryMB = serverOptions.MinMemoryMB;
                        config.EnableDetailedLogging = serverOptions.EnableDetailedLogging;
                        config.GracefulShutdownTimeout = serverOptions.GracefulShutdownTimeout;
                    });

                    // Register core services
                    services.AddSingleton<IJavaVersionService, JavaVersionService>();
                    services.AddSingleton<IProcessManagerService, ProcessManagerService>();
                    services.AddSingleton<IArgumentsBuilderService, ArgumentsBuilderService>();
                    services.AddSingleton<IStreamRelayService, StreamRelayService>();
                    services.AddSingleton<IConfigurationValidatorService, ConfigurationValidatorService>();

                    // Register strategy factories
                    services.AddSingleton<IJavaVersionStrategyFactory, JavaVersionStrategyFactory>();
                    services.AddSingleton<IMinecraftVersionStrategyFactory, MinecraftVersionStrategyFactory>();

                    // Register factories
                    services.AddSingleton<IProcessFactory, ProcessFactory>();

                    // Register the refactored hosted service
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