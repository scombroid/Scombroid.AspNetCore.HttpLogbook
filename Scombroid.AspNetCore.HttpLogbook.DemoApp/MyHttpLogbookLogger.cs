using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Scombroid.AspNetCore.HttpLogbook.DemoApp
{
    public class MyHttpLogbookLogger : IHttpLogbookLogger
    {
        private readonly ILogger Logger;
        const string RequestMessageTemplate = "##MY##Request {IpAddress} {RequestMethod} {RequestPath} {@Headers}";
        const string TraceRequestMessageTemplate = "##MY##Request {IpAddress} {RequestMethod} {RequestPath} {ContentType} {@RequestBody}";
        const string ResponseMessageTemplate = "##MY##Response {IpAddress} {StatusCode} {Elapsed}ms";
        const string TraceResponseMessageTemplate = "##MY##Response {IpAddress} {StatusCode} {Elapsed}ms {ContentType} {@ResponseBody}";

        public MyHttpLogbookLogger(ILogger<MyHttpLogbookLogger> logger)
        {
            Logger = logger;
        }

        public void LogRequest(LogLevel level, HttpRequest request)
        {
            LogRequest(level, request, null);
        }

        public void LogRequest(LogLevel level, HttpRequest request, string body)
        {
            var ipAddress = request?.HttpContext?.Connection?.RemoteIpAddress;
            if (body == null)
                Logger.LogInformation(RequestMessageTemplate, ipAddress, request.Method, request.Path, request.Headers.ToDictionary(k => k.Key, v => v.Value));
            else
                Logger.LogInformation(TraceRequestMessageTemplate, ipAddress, request.Method, request.Path, request.ContentType, body);
        }

        public void LogResponse(LogLevel level, HttpResponse response, TimeSpan timeTaken)
        {
            LogResponse(level, response, timeTaken, null, null);
        }

        public void LogResponse(LogLevel level, HttpResponse response, TimeSpan timeTaken, string contentType, string body)
        {
            var ipAddress = response?.HttpContext?.Connection?.RemoteIpAddress;
            if (contentType == null && body == null)
                Logger.LogInformation(ResponseMessageTemplate, ipAddress, response.StatusCode, timeTaken.TotalMilliseconds.ToString("0.0000"));
            else
                Logger.LogInformation(TraceResponseMessageTemplate, ipAddress, response.StatusCode, timeTaken.TotalMilliseconds.ToString("0.0000"), contentType, body);
        }
    }
}
