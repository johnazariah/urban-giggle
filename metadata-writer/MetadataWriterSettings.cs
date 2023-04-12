namespace metadata_writer
{
    public record class MetadataWriterSettings(Uri KustoEndpoint, string ManagedIdentityId, string KustoDatabaseName, string ContinuousExportName)
    {
        public static MetadataWriterSettings ReadSettings(string[] args)
        {
            var settingsParser = new SettingsParser(args);
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

            return new MetadataWriterSettings(kusto_cluster_uri, managed_identity, kusto_db_name, continuous_export_name);
        }
    }
}
