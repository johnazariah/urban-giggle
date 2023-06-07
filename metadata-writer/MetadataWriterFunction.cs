using metadata_writer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace metadata_writer_function
{
    public class MetadataWriterFunction
    {
        private readonly ILogger _logger;
        private readonly MetadataWriterService _service;

        public MetadataWriterFunction(MetadataWriterService service, ILoggerFactory loggerFactory)
        {
            _service = service;
            _logger = loggerFactory.CreateLogger<MetadataWriterFunction>();
        }

        [Function("MetadataWriterFunction")]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}; Next timer schedule at: {timer?.ScheduleStatus?.Next}");
            await _service.WriteMetadata();
        }
    }
}
