using metadata_writer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;

namespace metadata_writer_function
{
    public class MetadataWriterFunction
    {
        private readonly MetadataWriterService metadataWriterService;

        public MetadataWriterFunction(MetadataWriterService metadataWriterService)
        {
            this.metadataWriterService = metadataWriterService;
        }

        [Function("MetadataWriterFunction")]
        [SqlOutput("SliceIndex", "METADATA_DB_CONN_STR")]
        public async Task<List<SliceIndex>> Run([TimerTrigger("0 * * * * *")] TimerInfo timer, ILogger logger)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}; Next timer schedule at: {timer?.ScheduleStatus?.Next}");
            var slices = await metadataWriterService.FetchSlices(CancellationToken.None);

            return slices.ToList();
        }
    }
}
