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

            var filter = httpLogbookConfigFilter.FindByPath("/Simpletest", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/simpleTest", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/SiMpLeTeSt", "POST");
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

            var filter = httpLogbookConfigFilter.FindByPath("/doesnotexists", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/doesnotexists", "PUT");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/doesnotexists", "GET");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/doesnotexists", "DELETE");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/doesnotexists", "PATCH");
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

            var filter = httpLogbookConfigFilter.FindByPath("/testpostonly", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/testpostonly", "pOsT");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/testpostonly", "PUT");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.FindByPath("/testpostonly", "GET");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.FindByPath("/testpostonly", "DELETE");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.FindByPath("/testpostonly", "PATCH");
            Assert.Null(filter);
        }

        [Fact]
        public void RequestBodyEnabledTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.FindByPath("/requestbodyenabled", "POST");
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

            var filter = httpLogbookConfigFilter.FindByPath("/responsebodyenabled", "POST");
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

            var filter = httpLogbookConfigFilter.FindByPath("/MixMethods", "GET");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.True(filter.QueryString);
            Assert.True(filter.Request.Body);
            Assert.True(filter.Request.Headers);
            Assert.False(filter.Response.Body);
            Assert.False(filter.Response.Headers);

            filter = httpLogbookConfigFilter.FindByPath("/MixMethods", "POST");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.QueryString);
            Assert.False(filter.Request.Body);
            Assert.True(filter.Response.Body);

            filter = httpLogbookConfigFilter.FindByPath("/MixMethods", "DELETE");
            Assert.NotNull(filter);
            Assert.True(filter.Enabled);
            Assert.False(filter.QueryString);
            Assert.False(filter.Request.Body);
            Assert.False(filter.Response.Body);
        }
    }
}

