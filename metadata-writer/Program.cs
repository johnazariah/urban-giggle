//#define CLI_DEBUG
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace metadata_writer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
#if !CLI_DEBUG
                .ConfigureFunctionsWorkerDefaults()
#endif
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Configure the app configuration file.
                    config
                        .AddCommandLine(args)
                        .AddEnvironmentVariables()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // Configure logging.
                    logging
                        .AddConfiguration(hostingContext.Configuration.GetSection("Logging"))
                        .AddConsole()
                        .AddDebug();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services.
                    services.AddSingleton(MetadataWriterSettings.ReadSettings(hostContext.Configuration, args));
                    services.AddSingleton<MetadataWriterService>();
                });

            var host = builder.Build();
#if CLI_DEBUG
            await host.StartAsync();
            await host.StopAsync();
            await host.WaitForShutdownAsync();
#else
            await host.RunAsync();
#endif
        }
    }
}
