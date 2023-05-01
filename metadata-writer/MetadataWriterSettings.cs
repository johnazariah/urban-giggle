namespace metadata_writer
{
    public record class MetadataWriterSettings(
        Uri KustoEndpoint,
        string ManagedIdentityId,
        string KustoDatabaseName,
        string ContinuousExportName,
        string MetadataDbConnectionString,
        string MetadataTableName)
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

            var metadata_db_connection_string = settingsParser
                .GetSetting(
                    "metadata-db-connection-string",
                    "METADATA_DB_CONN_STR",
                    "MetadataDbConnectionString",
                    "Server=tcp:ccm-aks-dev.database.windows.net,1433;Initial Catalog=ccmmetadata;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Managed Identity\";User ID=\"5e8cfb80-8c2a-4b7e-99c6-12178769079b\"");

            var metadata_table_name = settingsParser
                .GetSetting(
                    "metadata-table-name",
                    "METADATA_TABLE_NAME",
                    "MetadataTableName",
                    "SliceIndex");

            return new MetadataWriterSettings(
                kusto_cluster_uri,
                managed_identity,
                kusto_db_name,
                continuous_export_name,
                metadata_db_connection_string,
                metadata_table_name);
        }
    }
}
