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
        public HttpRequest HttpRequest { get; set; }
        public string RequestBody { get; set; }
        public HttpResponse HttpResponse { get; set; }
        public string ResponseBody { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
