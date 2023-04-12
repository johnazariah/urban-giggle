using Azure.Identity;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using System.Data;

namespace metadata_writer
{
    public readonly record struct KustoQuery<T>(string DatabaseName, string Query, Func<IDataReader, T> ResultSelector, ClientRequestProperties ClientRequestProperties);

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
