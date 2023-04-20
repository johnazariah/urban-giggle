namespace metadata_writer
{
    public record class MetadataWriterSettings(
        Uri KustoEndpoint,
        string ManagedIdentityId,
        string KustoDatabaseName,
        string ContinuousExportName)
    {
        public static MetadataWriterSettings ReadSettings()
        {
            var kusto_cluster_uri =
                new Uri(Environment.GetEnvironmentVariable("KUSTO_CLUSTER_URI") ??
                "https://ccmxcostmanagement.westus2.kusto.windows.net");

            var managed_identity =
                Environment.GetEnvironmentVariable("MANAGED_IDENTITY") ??
                "5e8cfb80-8c2a-4b7e-99c6-12178769079b";

            var kusto_db_name =
                Environment.GetEnvironmentVariable("KUSTO_DB_NAME") ??
                "ccmx-usage-splits";

            var continuous_export_name =
                Environment.GetEnvironmentVariable("KUSTO_EXPORT_NAME") ??
                "ExportSplitUsageRecords";

            return new MetadataWriterSettings(
                kusto_cluster_uri,
                managed_identity,
                kusto_db_name,
                continuous_export_name);
        }
    }
}
