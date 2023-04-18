using metadata_writer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace metadata_writer_function
{
    public class MetadataWriterFunction
    {
        private readonly ILogger _logger;

        public MetadataWriterFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MetadataWriterFunction>();
        }

        [Function("MetadataWriterFunction")]
        public void Run([TimerTrigger("0 * * * * *")] TimerInfo timer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}; Next timer schedule at: {timer?.ScheduleStatus?.Next}");
            var settings = MetadataWriterSettings.ReadSettings(new string[] { });
            MetadataWriterService.WriteMetadata(settings, _logger).RunSynchronously();
        }
    }
}
