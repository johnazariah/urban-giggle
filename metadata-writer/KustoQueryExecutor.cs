using Azure.Identity;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using System.Data;

namespace metadata_writer
{
    public readonly record struct KustoQuery<T>
    {
        public string DatabaseName { get; }
        public string Query { get; }
        public Func<IDataReader, T> ResultSelector { get; }
        public ClientRequestProperties ClientRequestProperties { get; }

        public KustoQuery(string databaseName, string query, Func<IDataReader, T> resultSelector, ClientRequestProperties? clientRequestProperties = null)
        {
            DatabaseName = databaseName;
            Query = query;
            ResultSelector = resultSelector;
            ClientRequestProperties = clientRequestProperties ?? new ClientRequestProperties();
        }
    }

    public class KustoQueryExecutor : IDisposable
    {
        private ICslQueryProvider KustoQueryProvider { get; }

        public KustoQueryExecutor(Uri kustoServiceUri, string managedIdentityId)
        {
            var credential =
                new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityId });

            var connectionStringBuilder =
                new KustoConnectionStringBuilder(kustoServiceUri.ToString())
                .WithAadAzureTokenCredentialsAuthentication(credential);

            KustoQueryProvider = KustoClientFactory.CreateCslQueryProvider(connectionStringBuilder);
        }

        public async IAsyncEnumerable<T> ExecuteQueryAsync<T>(KustoQuery<T> kustoQuery)
        {
            var results = await KustoQueryProvider.ExecuteQueryAsync(
                kustoQuery.DatabaseName,
                kustoQuery.Query,
                kustoQuery.ClientRequestProperties);

            while (results.Read())
            {
                yield return kustoQuery.ResultSelector(results);
            }
        }

        public void Dispose() => KustoQueryProvider.Dispose();
    }
}
