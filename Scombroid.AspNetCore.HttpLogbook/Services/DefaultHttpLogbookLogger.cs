using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scombroid.AspNetCore.HttpLogbook.Services
{
    public class DefaultHttpLogbookLogger : IHttpLogbookLogger
    {
        private readonly ILogger Logger;

        public DefaultHttpLogbookLogger(ILogger<DefaultHttpLogbookLogger> logger)
        {
            Logger = logger;
        }

        public void Log(LogContext context)
        {
            const string RequestMessageTemplate = "{RequestMethod} {RequestPath} {StatusCode} {Elapsed}ms";
            Logger.Log(context.LogLevel, 
                RequestMessageTemplate,
                context.HttpContext?.Request?.Method,
                context.HttpContext?.Request?.Path,
                context.HttpContext?.Response?.StatusCode,
                context.Elapsed.TotalMilliseconds);
        }


        public void LogException(LogContext context, Exception ex)
        {
            const string ExceptionTemplate = "{RequestMethod} {RequestPath} {StatusCode} throws exception {Exception}";
            Logger.LogError(ex,
                    ExceptionTemplate,
                    context.HttpContext?.Request?.Method,
                    context.HttpContext?.Request?.Path,
                    context.HttpContext?.Response?.StatusCode,
                    ex);
        }
    }
}
