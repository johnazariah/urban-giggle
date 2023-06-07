using System;
using System.Diagnostics.Metrics;
using System.Reflection.PortableExecutable;
using Azure.Identity;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Agdistis
{
    public class TestBed
    {
        private readonly ILogger _logger;

        public TestBed(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TestBed>();
        }

        [Function("Function1")]
        public void Run([TimerTrigger("0 * * * * *")] MyInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");

            var credential =
                new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions { ManagedIdentityClientId = "5e8cfb80-8c2a-4b7e-99c6-12178769079b" }) ;

            var kcsb = new KustoConnectionStringBuilder("https://ccmxcostmanagement.westus2.kusto.windows.net")
                .WithAadAzureTokenCredentialsAuthentication(credential);

            using (var client = KustoClientFactory.CreateCslAdminProvider(kcsb))
            {
                var databasesShowCommand = CslCommandGenerator.GenerateDatabasesShowCommand();

                _logger.LogInformation("Executing {0}", databasesShowCommand);

                try
                {
                    using (var reader = client.ExecuteControlCommand(databasesShowCommand))
                    {
                        while (reader.Read())
                        {
                            _logger.LogInformation("DatabaseName={0}", reader.GetString(0));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Encountered Exception [{0}]", ex.Message);
                    throw;
                }
            }
        }
    }

    public record MyInfo(MyScheduleStatus ScheduleStatus, bool IsPastDue);

    public record MyScheduleStatus(DateTime Last, DateTime Next, DateTime LastUpdated);
}
