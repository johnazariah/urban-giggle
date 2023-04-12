using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;

namespace metadata_writer
{
    public readonly record struct Artefact(DateTime Timestamp, string Name, string Location);

    public class MetadataWriterService : BackgroundService
    {
        private readonly MetadataWriterSettings _settings;
        private readonly ILogger<MetadataWriterService> _logger;
        private readonly KustoQueryExecutor _kustoQueryExecutor;
        private readonly KustoQuery<Artefact> _kustoQuery;

        public MetadataWriterService(MetadataWriterSettings settings, ILogger<MetadataWriterService> logger)
        {
            _settings = settings;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _kustoQueryExecutor = new KustoQueryExecutor(_settings.KustoEndpoint, _settings.ManagedIdentityId);
            _kustoQuery = BuildQuery(_settings.KustoDatabaseName, _settings.ContinuousExportName);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MetadataWriterService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var count = 0;

                try
                {
                    _logger.LogInformation("Executing query to fetch exported artifacts from Kusto...");
                    await foreach (var artefact in _kustoQueryExecutor.ExecuteQueryAsync(_kustoQuery))
                    {
                        // Do something with these artefacts...
                        Console.WriteLine(artefact.ToString());
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occured while fetching artefacts from Kusto.");
                    continue;
                }

                _logger.LogInformation($"Fetched {count} artefacts from Kusto.");


                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("MetadataWriterService stopped.");
        }

        public override void Dispose()
        {
            _kustoQueryExecutor.Dispose();
            base.Dispose();
        }

        private static KustoQuery<Artefact> BuildQuery(string kusto_db_name, string continuous_export_name)
        {
            static Artefact readArtefact(IDataReader r) =>
                new(r.GetDateTime(0), r.GetString(1), r.GetString(2));

            var show_exported_artefacts_query = $".show continuous-export {continuous_export_name} exported-artifacts";
            return new KustoQuery<Artefact>(kusto_db_name, show_exported_artefacts_query, readArtefact, new Kusto.Data.Common.ClientRequestProperties());
        }
    }
}
