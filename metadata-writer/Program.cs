using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace metadata_writer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                    .ConfigureFunctionsWorkerDefaults();

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(MetadataWriterSettings.ReadSettings());
                services.AddSingleton<MetadataWriterService>();
                services.AddSingleton(services =>
                {
                    var settings = services.GetRequiredService<MetadataWriterSettings>();
                    return new KustoQueryExecutor(settings.KustoEndpoint, settings.ManagedIdentityId);
                });
            });

            await builder
                .Build()
                .RunAsync();
        }
    }
}
