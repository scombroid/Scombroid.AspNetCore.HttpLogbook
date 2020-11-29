using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scombroid.AspNetCore.HttpLogbook.Filters
{
    public class HttpLogbookMethodFilter
    {
        public HttpLogbookMessageFilter Request { get; set; }
            = new HttpLogbookMessageFilter();
        public HttpLogbookMessageFilter Response { get; set; }
            = new HttpLogbookMessageFilter();

        public LogLevel GetRequestLogLevel()
        {
            if (Request == null)
            {
                return LogLevel.None;
            }
            return Request.LogLevel;
        }

        public LogLevel GetResponseLogLevel()
        {
            if (Response == null)
            {
                return LogLevel.None;
            }
            return Response.LogLevel;
        }
    }
}
