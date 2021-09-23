using Microsoft.AspNetCore.Http;
using Scombroid.AspNetCore.HttpLogbook.Filters;
using System;

namespace Scombroid.AspNetCore.HttpLogbook
{
    public class LogContext
    {
        public Microsoft.Extensions.Logging.LogLevel LogLevel { get; set; }
        public string FilterPath { get; set; }
        public HttpLogbookMethodFilter Filter { get; set; }        
        public HttpContext HttpContext { get; set; }
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
