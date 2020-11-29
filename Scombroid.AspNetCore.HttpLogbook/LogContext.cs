using Microsoft.AspNetCore.Http;
using Scombroid.AspNetCore.HttpLogbook.Filters;
using System;

namespace Scombroid.AspNetCore.HttpLogbook
{
    public abstract class LogContext
    {
        public LogLevel LogLevel { get; set; }
        public string FilterPath { get; set; }
        public HttpLogbookMessageFilter MessageFilter { get; set; }
        public string Body { get; set; }        
    }

    public class RequestLogContext : LogContext
    {
        public HttpRequest HttpRequest { get; set; }
    }

    public class ResponseLogContext : LogContext
    {
        public HttpResponse HttpResponse { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

}
