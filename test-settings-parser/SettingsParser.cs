namespace test_setup_parser
{
    public class SettingsParserTests
    {
        private readonly SettingsParser.SettingsParser parser;

        public SettingsParserTests()
        {
            // create mock IConfigurationRoot object
            // set up environment variables section
            Environment.SetEnvironmentVariable("environmentVariableName", "value2");

            // create instance of SettingsParser and pass mock IConfigurationRoot object to constructor
            parser = new SettingsParser.SettingsParser(new[] { "--commandLineArgumentName=value1" } );
        }

        [Fact]
        public void GetSetting_WithCommandLineArgument_ReturnsExpectedValue()
        {
            var result = parser.GetSetting(
                "commandLineArgumentName",
                "environmentVariableName",
                "configurationSettingName");

            Assert.Equal("value1", result);
        }

        [Fact]
        public void GetSetting_WithEnvironmentVariable_ReturnsExpectedValue()
        {
            var result = parser.GetSetting(
                "",
                "environmentVariableName",
                "configurationSettingName");

            Assert.Equal("value2", result);
        }

        [Fact]
        public void GetSetting_WithConfigurationSetting_ReturnsExpectedValue()
        {
            // set up settings.json section
            var mockConfig = new Mock<IConfigurationRoot>();
            mockConfig.SetupGet(x => x["configurationSettingName"]).Returns("value3");
            typeof(SettingsParser.SettingsParser)?.GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(parser, mockConfig.Object);

            var result = parser.GetSetting(
                "",
                "",
                "configurationSettingName");

            Assert.Equal("value3", result);
        }

        [Fact]
        public void GetSetting_WithDefault_ReturnsDefaultValue()
        {
            var result = parser.GetSetting(
                "",
                "",
                "",
                "defaultValue");

            Assert.Equal("defaultValue", result);
        }

        [Fact]
        public void GetSetting_WithNullValue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                parser.GetSetting(
                "",
                "",
                "",
                null);
            });
        }

        [Fact]
        public void GetSetting_WithTransform_ReturnsTransformedValue()
        {
            // set up settings.json section
            var mockConfig = new Mock<IConfigurationRoot>();
            mockConfig.SetupGet(x => x["numericSetting"]).Returns("23");
            typeof(SettingsParser.SettingsParser)?.GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(parser, mockConfig.Object);

            var result = parser.GetSetting(
                "bogus_cla",
                "bogus_env",
                "numericSetting",
                null,
                x => int.Parse(x) * 2);

            Assert.Equal(46, result);
        }
    }
}
