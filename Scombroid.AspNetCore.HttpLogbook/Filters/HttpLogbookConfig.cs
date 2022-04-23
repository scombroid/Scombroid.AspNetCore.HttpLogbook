using System;
using System.Collections.Generic;

namespace Scombroid.AspNetCore.HttpLogbook.Filters
{
    public class HttpLogbookConfig
    {
        public bool Enabled { get; set; } = true;
        public int StreamBufferSize { get; set; } = 4096;

        public Dictionary<string, HttpLogbookPathFilter> Paths { get; set; }
            = new Dictionary<string, HttpLogbookPathFilter>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Options { get; set; }
    }
}

