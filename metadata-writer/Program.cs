using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data;

namespace metadata_writer
{
    public readonly record struct Artefact
    {
        public string Name { get; }
        public DateTime DateExported { get; }
        public string SourceTableName { get; }

        public Artefact(DateTime dateExported, string sourceTableName, string name)
        {
            Name = name;
            DateExported = dateExported;
            SourceTableName = sourceTableName;
        }

        public override string ToString() => 
            $"{{DateExported : {DateExported}; SourceTable : {SourceTableName}; Name : {Name}}}";
    }

    internal class Program
    {
        async static Task Main(string[] args)
        {
            Artefact readArtefact(IDataReader r) =>
                new(r.GetDateTime(0), r.GetString(1), r.GetString(2));

            var settingsParser = new SettingsParser.SettingsParser(args);

            var kusto_cluster_uri = settingsParser
                .GetSetting(
                    "kusto-cluster-uri", 
                    "KUSTO_CLUSTER_URI", 
                    "KustoClusterUri", 
                    "https://ccmxcostmanagement.westus2.kusto.windows.net",
                    u => new Uri(u));

            var managed_identity = settingsParser
                .GetSetting(
                    "managed-identity",
                    "MANAGED_IDENTITY",
                    "ManagedIdentity",
                    "5e8cfb80-8c2a-4b7e-99c6-12178769079b");

            var kusto_db_name = settingsParser
                .GetSetting(
                    "kusto-db-name",
                    "KUSTO_DB_NAME",
                    "KustoDbName",
                    "ccmx-usage-splits");

            var continuous_export_name = settingsParser
                .GetSetting(
                    "kusto-export-name",
                    "KUSTO_EXPORT_NAME",
                    "KustoExportName",
                    "ExportSplitUsageRecords");

            var show_exported_artefacts_query = $".show continuous-export {continuous_export_name} exported-artifacts";

            using var kqe = new KustoQueryExecutor.KustoQueryExecutor(kusto_cluster_uri, managed_identity);

            var kq = new KustoQueryExecutor.KustoQuery<Artefact>(kusto_db_name, show_exported_artefacts_query, readArtefact);

            await foreach (var artefact in kqe.ExecuteQueryAsync(kq))
            {
                Console.WriteLine(artefact.ToString());
            }
        }
    }
}