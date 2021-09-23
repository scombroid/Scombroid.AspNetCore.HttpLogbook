using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Scombroid.AspNetCore.HttpLogbook.Filters;
using Xunit;

namespace Scombroid.AspNetCore.HttpLogbook.Tests
{
    public class HttpLogbookConfigFilter_Tests
    {
        private HttpLogbookConfig ReadConfig()
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
            builder.Sources.Clear();
            builder.AddJsonFile("appsettings.json");
            var configRoot = builder.Build();
            var config = new HttpLogbookConfig();
            configRoot.Bind("HttpLogbook", config);
            return config;
        }

        private Mock<IOptionsMonitor<HttpLogbookConfig>> CreateConfigOptionsMonitor()
        {
            var config = ReadConfig();
            Assert.Equal(LogLevel.Information, config.LogLevel);
            var optionMonitorMock = new Mock<IOptionsMonitor<HttpLogbookConfig>>();
            optionMonitorMock.Setup(o => o.CurrentValue).Returns(config);
            return optionMonitorMock;
        }

        [Fact]
        public void ConfigTest()
        {
            Assert.NotNull(ReadConfig());
        }

        [Fact]
        public void DefaultMethodTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.Find("/Simpletest", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/simpleTest", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/SiMpLeTeSt", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);
        }

        [Fact]
        public void DefaultPathTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.Find("/doesnotexists", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/doesnotexists", "PUT");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/doesnotexists", "GET");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/doesnotexists", "DELETE");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/doesnotexists", "PATCH");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);
        }


        [Fact]
        public void PostOnlyTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.Find("/testpostonly", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "pOsT");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "PUT");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "GET");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "DELETE");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "PATCH");
            Assert.Null(filter);
        }

        [Fact]
        public void RequestBodyEnabledTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.Find("/requestbodyenabled", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.True(filter.Request.Body);
            Assert.False(filter.Response.Body);
        }

        [Fact]
        public void ResponseBodyEnabledTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.Find("/responsebodyenabled", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.True(filter.Response.Body);
        }

        [Fact]
        public void MixMethodsTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.Find("/MixMethods", "GET");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.True(filter.QueryString);
            Assert.True(filter.Request.Body);
            Assert.True(filter.Request.Headers);
            Assert.False(filter.Response.Body);
            Assert.False(filter.Response.Headers);

            filter = httpLogbookConfigFilter.Find("/MixMethods", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.QueryString);
            Assert.False(filter.Request.Body);
            Assert.True(filter.Response.Body);

            filter = httpLogbookConfigFilter.Find("/MixMethods", "DELETE");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.QueryString);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);
        }
    }
}

