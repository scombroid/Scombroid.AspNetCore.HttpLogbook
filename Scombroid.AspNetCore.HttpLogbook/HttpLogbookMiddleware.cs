﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        const string ExceptionMessageTemplate = "Exception {Elapsed}ms {@Exception}";
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
            try
            {
                var path = httpContext?.Request?.Path;
                var filter = LogbookFilter.Find(path, httpContext?.Request?.Method);

                // Get request body
                var requestBody = default(string);
                if (filter != null && filter.IsRequestBodyEnabled())
                {
                    requestBody = await GetRequestBodyAsync(httpContext.Request);
                    if (filter?.Request != null)
                    {
                        filter.Request.ApplyBodyMask(ref requestBody);
                    }
                }

                // process
                if (filter != null && filter.Enabled)
                {
                    if (filter.IsResponseBodyEnabled())
                    {
                        await ResponseBodyLogging(httpContext, path, filter.Response, sw, requestBody);
                    }
                    else
                    {
                        await ResponseLogging(httpContext, path, filter.Response, sw, requestBody);
                    }
                }
                else
                {
                    await _next(httpContext);
                }

                //LogLevel responseLogLevel = (filter != null) ? filter.GetResponseLogLevel() : LogLevel.None;
                //switch (responseLogLevel)
                //{
                //    case LogLevel.Trace:
                //        await ResponseTraceLogging(httpContext, path, filter.Response, sw);
                //        break;
                //    case LogLevel.Information:
                //        await ResponseInfoLogging(httpContext, path, filter.Response, sw);
                //        break;
                //    case LogLevel.None:
                //    default:
                //        await _next(httpContext);
                //        break;
                //}

            }
            // LogException() will always returns false, to allow the exception to bubble up
            catch (Exception ex) when (LogException(httpContext, ex, sw))
            {
            }
        }

        //private void RequestInfoLogging(HttpContext httpContext, PathString? path, HttpLogbookMessageFilter messageFilter)
        //{
        //    LogbookService.LogRequest(new RequestLogContext()
        //    {
        //        LogLevel = LogLevel.Information,
        //        FilterPath = path?.Value,
        //        MessageFilter = messageFilter,
        //        HttpRequest = httpContext.Request,
        //        Body = null
        //    });
        //}

        //private async Task RequestTraceLogging(HttpContext httpContext, PathString? path, HttpLogbookMessageFilter messageFilter)
        //{
        //    string requestBody = await GetRequestBodyAsync(httpContext.Request);
        //    if (messageFilter != null)
        //    {
        //        messageFilter.ApplyBodyMask(ref requestBody);
        //    }

        //    LogbookService.LogRequest(new RequestLogContext()
        //    {
        //        LogLevel = LogLevel.Trace,
        //        FilterPath = path?.Value,
        //        MessageFilter = messageFilter,
        //        HttpRequest = httpContext.Request,
        //        Body = requestBody
        //    });
        //}

        private async Task ResponseLogging(HttpContext httpContext, PathString? path, HttpLogbookMessageFilter messageFilter, Stopwatch sw, string requestBody)
        {
            await _next(httpContext);
            sw.Stop();

            LogbookService.Log(new LogContext()
            {
                LogLevel = LogbookFilter.LogLevel,
                FilterPath = path?.Value,
                MessageFilter = messageFilter,
                HttpRequest = httpContext.Request,
                RequestBody = requestBody,
                HttpResponse = httpContext.Response,
                ResponseBody = null,
                Elapsed = sw.Elapsed                
            });
        }

        private async Task ResponseBodyLogging(HttpContext httpContext, PathString? path, HttpLogbookMessageFilter messageFilter, Stopwatch sw, string requestBody)
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
                    if (messageFilter != null)
                    {
                        messageFilter.ApplyBodyMask(ref responseBody);
                    }
                    sw.Stop();

                    LogbookService.Log(new LogContext()
                    {
                        LogLevel = LogbookFilter.LogLevel,
                        FilterPath = path?.Value,
                        MessageFilter = messageFilter,
                        HttpRequest = httpContext.Request,
                        RequestBody = requestBody,
                        HttpResponse = httpContext.Response,
                        ResponseBody = responseBody,
                        Elapsed = sw.Elapsed                        
                    });
                }
            }
            finally
            {
                httpContext.Response.Body = originalResponseBody;
            }
        }

        private bool LogException(HttpContext httpContext, Exception ex, Stopwatch sw)
        {
            sw.Stop();
            Logger.LogError(ex, ExceptionMessageTemplate, sw.ElapsedMilliseconds, ex);
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