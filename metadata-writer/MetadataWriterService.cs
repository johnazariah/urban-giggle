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

    public class MetadataWriterService
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly MetadataWriterSettings _settings;
        private readonly ILogger _logger;
        private readonly KustoQueryExecutor _kustoQueryExecutor;
        private readonly KustoQuery<Artefact> _kustoQuery;

        private readonly Regex parser = new(@"https://(?<server>\w+).(?<url>[.\w]*)/(?<container>\w+)/(?<channel>\w+)/(?<exportDate>\w{8})/(?<exportTime>\w{4})/(?<usageDateKey>\w{8})/(?<filepath>[-\w]+).(?<extn>\w+)");

        private static KustoQuery<Artefact> BuildQuery(string kusto_db_name, string continuous_export_name)
        {
            var show_exported_artefacts_query = $".show continuous-export {continuous_export_name} exported-artifacts | where Timestamp > ago(24h) | order by Timestamp desc";
            return new KustoQuery<Artefact>(kusto_db_name, show_exported_artefacts_query, Artefact.Read, new Kusto.Data.Common.ClientRequestProperties());
        }

        public MetadataWriterService(MetadataWriterSettings settings, ILogger logger, KustoQueryExecutor kustoQueryExecutor)
        {
            _settings = settings;
            _logger = logger;
            _kustoQueryExecutor = kustoQueryExecutor;
            _kustoQuery = BuildQuery(_settings.KustoDatabaseName, _settings.ContinuousExportName);
        }

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

        public async Task<HashSet<SliceIndex>> FetchSlices(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"MetadataWriterService started. [{_settings}]");

            var toSlice = ToSliceIndex(DateTime.UtcNow);

            try
            {
                _logger.LogInformation($"Executing [{_kustoQuery}] to fetch exported artifacts from Kusto");

                var slices =
                    from artefact in _kustoQueryExecutor.ExecuteQueryAsync(_kustoQuery)
                    let slice = toSlice(artefact)
                    where slice is not null
                    select slice;

                var uniqueSlices =
                    await slices.ToHashSetAsync(cancellationToken: cancellationToken);

                _logger.LogInformation($"Fetched {uniqueSlices.Count} unique artefacts from Kusto.");

                return uniqueSlices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching artefacts from Kusto.");
                throw;
            }
        }
    }
}
