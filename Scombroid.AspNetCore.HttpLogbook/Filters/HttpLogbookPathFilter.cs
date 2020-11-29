using System;
using System.Collections.Generic;
using System.Text;

namespace Scombroid.AspNetCore.HttpLogbook.Filters
{
    public class HttpLogbookPathFilter
    {
        public Dictionary<string, HttpLogbookMethodFilter> Methods { get; set; }
            = new Dictionary<string, HttpLogbookMethodFilter>(StringComparer.OrdinalIgnoreCase);
    }
}
