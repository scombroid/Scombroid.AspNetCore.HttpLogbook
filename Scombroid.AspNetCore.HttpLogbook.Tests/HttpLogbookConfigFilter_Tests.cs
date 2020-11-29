using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Scombroid.AspNetCore.HttpLogbook.Filters;
using System.IO;
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
        public void SimpleTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.Find("/Simpletest", "POST");
            Assert.NotNull(filter);
            Assert.Equal(HttpLogbook.LogLevel.Information, filter.Request.LogLevel);

            filter = httpLogbookConfigFilter.Find("/simpleTest", "POST");
            Assert.NotNull(filter);
            Assert.Equal(HttpLogbook.LogLevel.Information, filter.Request.LogLevel);

            filter = httpLogbookConfigFilter.Find("/SiMpLeTeSt", "POST");
            Assert.NotNull(filter);
            Assert.Equal(HttpLogbook.LogLevel.Information, filter.Request.LogLevel);
        }

        [Fact]
        public void PostOnlyTest()
        {
            var optionMonitorMock = CreateConfigOptionsMonitor();
            var loggerMock = new Mock<ILogger<HttpLogbookConfigFilter>>();
            var httpLogbookConfigFilter = new HttpLogbookConfigFilter(loggerMock.Object, optionMonitorMock.Object);

            var filter = httpLogbookConfigFilter.Find("/testpostonly", "POST");
            Assert.NotNull(filter);
            Assert.Equal(HttpLogbook.LogLevel.Information, filter.Request.LogLevel);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "pOsT");
            Assert.NotNull(filter);
            Assert.Equal(HttpLogbook.LogLevel.Information, filter.Request.LogLevel);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "PUT");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "GET");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "DELETE");
            Assert.Null(filter);

            filter = httpLogbookConfigFilter.Find("/testpostonly", "PATCH");
            Assert.Null(filter);
        }
    }
}

