using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace Scombroid.AspNetCore.HttpLogbook.Services
{
    public class DefaultHttpLogbookLogger : IHttpLogbookLogger
    {
        private readonly ILogger Logger;
        const string RequestMessageTemplate = "Request {IpAddress} {RequestMethod} {RequestPath}";
        const string TraceRequestMessageTemplate = "Request {IpAddress} {RequestMethod} {RequestPath} {@ContentType} {@RequestBody}";
        const string ResponseMessageTemplate = "Response {IpAddress} {StatusCode} {Elapsed}ms";
        const string TraceResponseMessageTemplate = "Response {IpAddress} {StatusCode} {Elapsed}ms {@ContentType} {@ResponseBody}";

        public DefaultHttpLogbookLogger(ILogger<DefaultHttpLogbookLogger> logger)
        {
            Logger = logger;
        }

        public void LogRequest(RequestLogContext context)
        {
            LogRequest(context.LogLevel, context.HttpRequest, context.Body);
        }

        private void LogRequest(LogLevel level, HttpRequest request, string body)
        {
            var ipAddress = request?.HttpContext?.Connection?.RemoteIpAddress;
            if (body == null)
                Logger.LogInformation(RequestMessageTemplate, ipAddress, request.Method, request.Path, body);
            else
                Logger.LogInformation(TraceRequestMessageTemplate, ipAddress, request.Method, request.Path, request.ContentType, body);
        }

        public void LogResponse(ResponseLogContext context)
        {
            LogResponse(context.LogLevel, context.HttpResponse, context.Elapsed, context.HttpResponse.ContentType, context.Body);
        }

        private void LogResponse(LogLevel level, HttpResponse response, TimeSpan elapsed, string contentType, string body)
        {
            var ipAddress = response?.HttpContext?.Connection?.RemoteIpAddress;
            if (contentType == null && body == null)
                Logger.LogInformation(ResponseMessageTemplate, ipAddress, response.StatusCode, elapsed.TotalMilliseconds.ToString("0.0000"));
            else
                Logger.LogInformation(TraceResponseMessageTemplate, ipAddress, response.StatusCode, elapsed.TotalMilliseconds.ToString("0.0000"), contentType, body);
        }
    }
}
