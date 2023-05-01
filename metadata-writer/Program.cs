//#define CLI_DEBUG
#if CLI_DEBUG
using Microsoft.Extensions.DependencyInjection;
#endif

using Microsoft.Extensions.Configuration;
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
#if CLI_DEBUG
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services.
                    services.AddSingleton(MetadataWriterSettings.ReadSettings(args));
                    services.AddHostedService<MetadataWriterService>();
#endif
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
