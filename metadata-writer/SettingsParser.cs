﻿using Functional;
using Microsoft.Extensions.Configuration;

namespace metadata_writer
{
    public class SettingsParser
    {
        internal readonly IConfigurationRoot config;

        public SettingsParser(string[] args) => config =
            new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

        public string GetSetting(
            string commandLineArgumentName,
            string environmentVariableName,
            string configurationSettingName,
            string? defaultValue = null) =>
                config[commandLineArgumentName]
                ?? config[environmentVariableName]
                ?? config[configurationSettingName]
                ?? defaultValue
                ?? throw new ArgumentNullException($"No value specified for [{commandLineArgumentName}], [{environmentVariableName}] or [{configurationSettingName}]!");

        public T GetSetting<T>(
            string commandLineArgumentName,
            string environmentVariableName,
            string configurationSettingName,
            string? defaultValue,
            Func<string, T> transformer) =>
                GetSetting(
                    commandLineArgumentName,
                    environmentVariableName,
                    configurationSettingName,
                    defaultValue)
                .PipeTo(transformer);
    }
}
