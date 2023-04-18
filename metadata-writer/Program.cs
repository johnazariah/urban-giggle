//#define CLI_DEBUG
#if CLI_DEBUG
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#endif
using Microsoft.Extensions.Hosting;

namespace metadata_writer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
#if !CLI_DEBUG
            await new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build()
                .RunAsync();
#else
            var builder = new HostBuilder()
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
                    services.AddSingleton(MetadataWriterSettings.ReadSettings(args));
                    services.AddHostedService<MetadataWriterService>();
                });

            var host = builder.Build();

            await host.StartAsync();
            await host.StopAsync();
            await host.WaitForShutdownAsync();
#endif
        }
    }
}
