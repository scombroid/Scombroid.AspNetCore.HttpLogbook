using Microsoft.AspNetCore.Http;
using System;

namespace Scombroid.AspNetCore.HttpLogbook
{
    public interface IHttpLogbookLogger
    {
        void Log(LogContext context);
        void LogException(Exception ex, HttpContext httpContext, TimeSpan elapsed);
    }
}
