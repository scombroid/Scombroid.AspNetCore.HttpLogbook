using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Scombroid.AspNetCore.HttpLogbook.Filters;

namespace Scombroid.AspNetCore.HttpLogbook
{
    internal class HttpLogbookMiddleware
    {
        const string ExceptionMessageTemplate = "Exception {IpAddress} {Elapsed}ms {@Exception}";
        private readonly RequestDelegate _next;
        private readonly ILogger Logger;
        private readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager;
        private readonly IHttpLogbookFilter LogbookFilter;
        private readonly IHttpLogbookLogger LogbookService;
        private readonly HttpLogbookMiddlewareOptions Options;
        public HttpLogbookMiddleware(RequestDelegate next, ILogger<HttpLogbookMiddleware> logger,
            IHttpLogbookFilter logbookFilter,
            IHttpLogbookLogger logbookService,
            HttpLogbookMiddlewareOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            Logger = logger;
            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            LogbookFilter = logbookFilter;
            LogbookService = logbookService;
            Options = options;
        }

        private int BufferSize
        {
            get 
            {
                return Options.BufferSize;
            }
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));
            var sw = Stopwatch.StartNew();
            string ipAddress = null;
            try
            {
                var filter = LogbookFilter.Find(httpContext?.Request?.Path, httpContext?.Request?.Method);
                
                // process request
                LogLevel requestLogLevel = (filter != null) ? filter.GetRequestLogLevel(): LogLevel.None;                
                switch (requestLogLevel)
                {
                    case LogLevel.Trace:
                        await RequestTraceLogging(filter.Request, httpContext);
                        break;
                    case LogLevel.Information:
                        RequestInfoLogging(httpContext);
                        break;
                    case LogLevel.None:
                    default:
                        break;
                }

                // process response
                LogLevel responseLogLevel = (filter != null) ? filter.GetResponseLogLevel() : LogLevel.None;
                switch (responseLogLevel)
                {
                    case LogLevel.Trace:
                        await ResponseTraceLogging(filter.Response, httpContext, sw);
                        break;
                    case LogLevel.Information:
                        await ResponseInfoLogging(httpContext, sw);
                        break;
                    case LogLevel.None:
                    default:
                        await _next(httpContext);
                        break;
                }

            }
            // LogException() will always returns false, to allow the exception to bubble up
            catch (Exception ex) when (LogException(ipAddress, httpContext, ex, sw))
            {
            }
        }

        private void RequestInfoLogging(HttpContext httpContext)
        {
            LogbookService.LogRequest(LogLevel.Information, httpContext.Request);
        }

        private async Task RequestTraceLogging(HttpLogbookMessageFilter rule, HttpContext httpContext)
        {
            string requestBody = await GetRequestBodyAsync(httpContext.Request);
            if (rule != null)
            {
                rule.ApplyBodyMask(ref requestBody);
            }
            LogbookService.LogRequest(LogLevel.Trace, httpContext.Request, requestBody);
        }

        private async Task ResponseInfoLogging(HttpContext httpContext, Stopwatch sw)
        {
            await _next(httpContext);
            sw.Stop();
            LogbookService.LogResponse(LogLevel.Information, httpContext.Response, sw.Elapsed);
        }

        private async Task ResponseTraceLogging(HttpLogbookMessageFilter rule, HttpContext httpContext, Stopwatch sw)
        {
            Stream originalResponseBody = httpContext.Response.Body;
            try
            {
                using (MemoryStream newResponseBody = RecyclableMemoryStreamManager.GetStream())
                {
                    // swap the context response body stream with the memory stream
                    httpContext.Response.Body = newResponseBody;
                    await _next(httpContext);
                    // reset position to 0
                    newResponseBody.Seek(0, SeekOrigin.Begin);
                    // Copy the response body to the original body stream
                    await newResponseBody.CopyToAsync(originalResponseBody);
                    // Get response body in text format
                    string responseBody = GetResponseBody(httpContext.Response, BufferSize);
                    if (rule != null)
                    {
                        rule.ApplyBodyMask(ref responseBody);
                    }
                    sw.Stop();
                    LogbookService.LogResponse(LogLevel.Trace, httpContext.Response, sw.Elapsed, httpContext.Response.ContentType, responseBody);
                }
            }
            finally
            {
                httpContext.Response.Body = originalResponseBody;
            }
        }

        private bool LogException(string ipAddress, HttpContext httpContext, Exception ex, Stopwatch sw)
        {
            sw.Stop();
            Logger.LogError(ex, ExceptionMessageTemplate, ipAddress, sw.ElapsedMilliseconds, ex);
            return false; // LogException() will always returns false, to allow the exception to bubble up
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            //request.EnableRewind();
            using (var requestStream = RecyclableMemoryStreamManager.GetStream())
            {
                await request.Body.CopyToAsync(requestStream);
                request.Body.Seek(0, SeekOrigin.Begin);
                return ReadStreamInChunks(requestStream, BufferSize);
            }
        }

        private static string GetResponseBody(HttpResponse response, int bufferSize)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            return ReadStreamInChunks(response.Body, bufferSize);
        }

        private static string ReadStreamInChunks(Stream stream, int bufferSize)
        {
            stream.Seek(0, SeekOrigin.Begin);
            string result;
            using (var textWriter = new StringWriter())
            using (var reader = new StreamReader(stream))
            {
                var readChunk = new char[bufferSize];
                int readChunkLength;
                //do while: is useful for the last iteration in case readChunkLength < chunkLength
                do
                {
                    readChunkLength = reader.ReadBlock(readChunk, 0, bufferSize);
                    textWriter.Write(readChunk, 0, readChunkLength);
                } while (readChunkLength > 0);
                result = textWriter.ToString();
            }
            return result;
        }
    }
}