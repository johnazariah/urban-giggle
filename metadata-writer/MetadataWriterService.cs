using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.RegularExpressions;

namespace metadata_writer
{
    public readonly record struct Artefact(DateTime Timestamp, string Name, string Location, long NumRecords, long SizeInBytes)
    {
        public static Artefact Read(IDataReader r) =>
            new(r.GetDateTime(0), r.GetString(1), r.GetString(2), r.GetInt64(3), r.GetInt64(4));
    }

    public record SliceIndex(string? StreamName, string? Slice, string? SlicePath, DateTime? PublishTime, string? RunId, long? RecordCount);

    public class MetadataDbContext : DbContext
    {
        private readonly string _connectionString;
        private readonly string _tableName;

        public DbSet<SliceIndex>? SliceIndices { get; set; }

        public MetadataDbContext(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            _tableName = tableName;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<SliceIndex>()
                .ToTable(_tableName)
                .HasKey(x => new { x.Slice, x.RunId });
        }
    }

    public class MetadataWriterService : IHostedService
    {
        private readonly MetadataWriterSettings _settings;
        private readonly ILogger<MetadataWriterService> _logger;
        private readonly KustoQueryExecutor _kustoQueryExecutor;
        private readonly KustoQuery<Artefact> _kustoQuery;
        private readonly MetadataDbContext _metadataDbContext;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        private readonly Regex parser = new Regex(@"https://(?<server>\w+).(?<url>[.\w]*)/(?<container>\w+)/(?<channel>\w+)/(?<exportDate>\w{8})/(?<exportTime>\w{4})/(?<usageDateKey>\w{8})/(?<filepath>[-\w]+).(?<extn>\w+)");

        public MetadataWriterService(MetadataWriterSettings settings, ILogger<MetadataWriterService> logger)
        {
            _settings = settings;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _kustoQueryExecutor = new KustoQueryExecutor(_settings.KustoEndpoint, _settings.ManagedIdentityId);
            _kustoQuery = BuildQuery(_settings.KustoDatabaseName, _settings.ContinuousExportName);
            _metadataDbContext = new MetadataDbContext(_settings.MetadataDbConnectionString, _settings.MetadataTableName);
        }

        private Func<SliceIndex, Task> WriteSliceRecord(CancellationToken stoppingToken) =>
            async slice =>
            {
                var existingRecord = _metadataDbContext.SliceIndices?.FirstOrDefault(e => e.SlicePath == slice.SlicePath);
                if (existingRecord != null)
                {
                    _logger.LogInformation($"Found record for {slice.SlicePath} already. Skipping");
                    return;
                }

                _logger.LogInformation($"Writing {slice} to database");
                await _metadataDbContext.AddAsync(slice, cancellationToken: stoppingToken);
                await _metadataDbContext.SaveChangesAsync(cancellationToken: stoppingToken);
            };

        private Func<Artefact, SliceIndex?> ToSliceIndex(DateTime publishTime) =>
            artefact =>
            {
                var match = parser.Match(artefact.Location);
                if (match.Success)
                {
                    var server = match.Groups["server"].Value;
                    var url = match.Groups["url"].Value;
                    var container = match.Groups["container"].Value;
                    var channel = match.Groups["channel"].Value;
                    var exportDate = match.Groups["exportDate"].Value;
                    var exportTime = match.Groups["exportTime"].Value;
                    var usageDateKey = match.Groups["usageDateKey"].Value;
                    var filepath = match.Groups["filepath"].Value;
                    var extn = match.Groups["extn"].Value;

                    return new SliceIndex(
                        StreamName: "AKSUtilizationSplit-JA",
                        Slice: usageDateKey,
                        SlicePath: $"wasbs://{container}@{server}.{url}/{channel}/{exportDate}/{exportTime}/{usageDateKey}/",
                        PublishTime: publishTime,
                        RunId: $"{exportDate}{exportTime}",
                        RecordCount: artefact.NumRecords
                    );
                }

                return null;
            };

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MetadataWriterService started.");

            var toSlice = ToSliceIndex(DateTime.UtcNow);
            var writeSlice = WriteSliceRecord(cancellationToken);

            try
            {
                _logger.LogInformation("Executing query to fetch exported artifacts from Kusto...");

                var slices = 
                    from artefact in _kustoQueryExecutor.ExecuteQueryAsync(_kustoQuery)
                    let slice = toSlice(artefact)
                    where slice is not null
                    select slice;

                var uniqueSlices =
                    await slices.ToHashSetAsync(cancellationToken: cancellationToken);

                // WARNING: can't do this because EF chokes on parallel writes
                // await Task.WhenAll(uniqueSlices.Select(writeSlice));
                foreach ( var slice in uniqueSlices )
                {
                    await writeSlice(slice);
                }

                _logger.LogInformation($"Fetched {uniqueSlices.Count} unique artefacts from Kusto.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while fetching artefacts from Kusto.");
            }

            _logger.LogInformation("MetadataWriterService stopped.");
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping the service.");

            _cts.Cancel();
            _kustoQueryExecutor.Dispose();

            return Task.CompletedTask;
        }

        private static KustoQuery<Artefact> BuildQuery(string kusto_db_name, string continuous_export_name)
        {
            var show_exported_artefacts_query = $".show continuous-export {continuous_export_name} exported-artifacts | order by Timestamp desc | where Timestamp > ago(24h)";
            return new KustoQuery<Artefact>(kusto_db_name, show_exported_artefacts_query, Artefact.Read, new Kusto.Data.Common.ClientRequestProperties());
        }
    }
}
