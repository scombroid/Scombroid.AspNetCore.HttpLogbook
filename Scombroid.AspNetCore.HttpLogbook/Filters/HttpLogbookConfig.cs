using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Scombroid.AspNetCore.HttpLogbook.Filters
{
    public class HttpLogbookConfig
    {
        public int StreamBufferSize { get; set; } = 4096;

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public Dictionary<string, HttpLogbookPathFilter> Paths { get; set; }
            = new Dictionary<string, HttpLogbookPathFilter>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, HttpLogbookPathFilter> Actions { get; set; }
            = new Dictionary<string, HttpLogbookPathFilter>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Options { get; set; }
    }
}

