using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Scombroid.AspNetCore.HttpLogbook.Filters;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Scombroid.AspNetCore.HttpLogbook
{
    internal class HttpLogbookMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager;
        private readonly IHttpLogbookFilter LogbookFilter;
        private readonly IHttpLogbookLogger LogbookService;
        public HttpLogbookMiddleware(RequestDelegate next,
            IHttpLogbookFilter logbookFilter,
            IHttpLogbookLogger logbookService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            LogbookFilter = logbookFilter;
            LogbookService = logbookService;
        }

        public async Task Invoke(HttpContext httpContext, IOptions<HttpLogbookConfig> configOption)
        {
            var config = configOption.Value;
            var bufferSize = config.StreamBufferSize;

            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));
            var sw = Stopwatch.StartNew();

            var logContext = new LogContext()
            {
                LogLevel = config.LogLevel
            };

            try
            {
                logContext.FilterPath = httpContext?.Request?.Path;
                logContext.Filter = LogbookFilter.Find(logContext.FilterPath, httpContext?.Request?.Method);
                logContext.HttpContext = httpContext;

                // Get request body
                if (logContext.Filter != null && (logContext.Filter?.Request?.Body).GetValueOrDefault(false))
                {
                    var requestBody = await GetRequestBodyAsync(httpContext.Request, bufferSize);
                    if (logContext.Filter.Request != null)
                    {
                        logContext.Filter.Request.ApplyBodyMask(ref requestBody);
                    }

                    logContext.RequestBody = requestBody;
                }

                // process
                if (logContext.Filter != null && logContext.Filter.Enabled)
                {
                    if ((logContext.Filter.Response?.Body).GetValueOrDefault(false))
                    {
                        var responseBody = await ResponseBodyLogging(httpContext, bufferSize);
                        if (logContext.Filter.Response != null)
                        {
                            logContext.Filter.Response.ApplyBodyMask(ref responseBody);
                        }
                        logContext.ResponseBody = responseBody;
                    }
                    else
                    {
                        await _next(httpContext);
                    }
                    sw.Stop();
                    
                    logContext.Elapsed = sw.Elapsed;
                    LogbookService.Log(logContext);
                }
                else
                {
                    await _next(httpContext);
                }
            }
            // LogException() will always returns false, to allow the exception to bubble up
            catch (Exception ex) when ( LogException(ex, sw, logContext) )
            {
            }
        }

        private async Task<string> ResponseBodyLogging(HttpContext httpContext, int bufferSize)
        {
            string responseBody;
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
                    responseBody = GetResponseBody(httpContext.Response, bufferSize);
                }
            }
            finally
            {
                httpContext.Response.Body = originalResponseBody;
            }
            return responseBody;
        }

        private bool LogException(Exception ex, Stopwatch sw, LogContext logContext)
        {
            sw.Stop();
            logContext.Elapsed = sw.Elapsed;
            LogbookService.LogException(logContext, ex);
            return false; // LogException() will always returns false, to allow the exception to bubble up
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request, int bufferSize)
        {
            request.EnableBuffering();
            //request.EnableRewind();
            using (var requestStream = RecyclableMemoryStreamManager.GetStream())
            {
                await request.Body.CopyToAsync(requestStream);
                request.Body.Seek(0, SeekOrigin.Begin);
                return ReadStreamInChunks(requestStream, bufferSize);
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