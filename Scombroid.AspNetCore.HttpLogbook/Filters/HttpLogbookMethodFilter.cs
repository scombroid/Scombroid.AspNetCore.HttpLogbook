using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scombroid.AspNetCore.HttpLogbook.Filters
{
    public class HttpLogbookMethodFilter
    {
        public bool Enabled { get; set; } = false;
        public bool QueryString { get; set; } = false;
        public HttpLogbookMessageFilter Request { get; set; }
            = new HttpLogbookMessageFilter();
        public HttpLogbookMessageFilter Response { get; set; }
            = new HttpLogbookMessageFilter();
    }
}
