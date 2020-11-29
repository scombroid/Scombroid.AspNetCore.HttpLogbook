using Microsoft.AspNetCore.Http;
using System;

namespace Scombroid.AspNetCore.HttpLogbook
{
    public interface IHttpLogbookLogger
    {
        void LogRequest(LogLevel level, HttpRequest request);
        void LogRequest(LogLevel level, HttpRequest request, string body);

        void LogResponse(LogLevel level, HttpResponse response, TimeSpan timeTaken);
        void LogResponse(LogLevel level, HttpResponse response, TimeSpan timeTaken, string contentType, string body);
    }
}
