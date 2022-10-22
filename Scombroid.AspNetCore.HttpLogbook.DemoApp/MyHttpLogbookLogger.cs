using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Scombroid.AspNetCore.HttpLogbook.DemoApp
{
    public class MyHttpLogbookLogger : IHttpLogbookLogger
    {
        private readonly ILogger Logger;
        
        public MyHttpLogbookLogger(ILogger<MyHttpLogbookLogger> logger)
        {
            Logger = logger;
        }

        public void Log(LogContext context)
        {
            var scopes = new Dictionary<string, object>
            {
                ["IpAddress"] = context.HttpContext?.Connection?.RemoteIpAddress,
                //["Headers"] = context.HttpRequest?.Headers,
                ["Method"] = context.HttpContext?.Request?.Method,
                ["Path"] = context.HttpContext?.Request?.Path,
                ["ContentType"] = context.HttpContext?.Request?.ContentType,
                ["QueryString"] = context.HttpContext?.Request?.QueryString,
                ["RequestBody"] = context.RequestBody,
                ["ResponseBody"] = context.ResponseBody,
                ["StatusCode"] = context.HttpContext?.Response?.StatusCode,
                ["Elapsed"] = context.Elapsed
            };

            using (Logger.BeginScope(scopes))
            {
                const string RequestMessageTemplate = "##MY## {Method} {Path} {StatusCode} {@scopes}";
                Logger.LogInformation(RequestMessageTemplate,
                    context.HttpContext?.Request?.Method,
                    context.HttpContext?.Request?.Path,
                    context.HttpContext?.Response?.StatusCode,
                    scopes);
            }
        }

        public void LogException(LogContext context, Exception ex)
        {
            const string ExceptionTemplate = "##MY## {Method} {Path} {StatusCode} throws exception {Exception}";
            Logger.LogError(ex,
                    ExceptionTemplate,
                    context.HttpContext?.Request?.Method,
                    context.HttpContext?.Request?.Path,
                    context.HttpContext?.Response?.StatusCode,
                    ex);
        }
    }
}
