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
                ["IpAddress"] = context.HttpRequest?.HttpContext?.Connection?.RemoteIpAddress,
                //["Headers"] = context.HttpRequest?.Headers,
                ["RequestMethod"] = context.HttpRequest?.Method,
                ["RequestPath"] = context.HttpRequest?.Path,
                ["ResponseContentType"] = context.HttpRequest?.ContentType,
                ["QueryString"] = context.HttpRequest?.QueryString,
                ["RequestBody"] = context.RequestBody,
                ["ResponseBody"] = context.ResponseBody,
                ["StatusCode"] = context.HttpResponse?.StatusCode,
                ["Elapsed"] = context.Elapsed
            };

            using (Logger.BeginScope(scopes))
            {
                const string RequestMessageTemplate = "##MY## {RequestMethod} {RequestPath} {StatusCode} {@scopes}";
                Logger.LogInformation(RequestMessageTemplate,
                    context.HttpRequest?.Method,
                    context.HttpRequest?.Path,
                    context.HttpResponse?.StatusCode,
                    scopes);
            }
        }
    }
}
