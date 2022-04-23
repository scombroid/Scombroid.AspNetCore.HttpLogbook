using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
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
            var bufferSize = (config?.StreamBufferSize).GetValueOrDefault(4096);
            var logLevel = (config?.LogLevel).GetValueOrDefault(LogLevel.Information);

            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (logLevel == LogLevel.None)
            {
                await _next(httpContext); // disabled, do nothing
            }
            else
            {
                await Process(httpContext, logLevel, bufferSize);
            }
        }

        private async Task Process(HttpContext httpContext, LogLevel logLevel, int bufferSize)
        {
            var sw = Stopwatch.StartNew();
            var logContext = new LogContext()
            {
                LogLevel = logLevel
            };

            Stream originalResponseBody = httpContext.Response.Body;
            try
            {
                using (MemoryStream newResponseBody = RecyclableMemoryStreamManager.GetStream())
                {
                    // swap the context response body stream with the memory stream
                    httpContext.Response.Body = newResponseBody;

                    // enable buffering
                    // https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httprequestrewindextensions.enablebuffering?view=aspnetcore-6.0#microsoft-aspnetcore-http-httprequestrewindextensions-enablebuffering(microsoft-aspnetcore-http-httprequest)
                    // Ensure the requestBody can be read multiple times. Normally buffers request bodies in memory; writes requests larger than 30K bytes to disk.
                    httpContext.Request.EnableBuffering();

                    // execute next
                    await _next(httpContext);

                    // reset position to 0
                    newResponseBody.Seek(0, SeekOrigin.Begin);

                    // Copy the response body to the original body stream
                    await newResponseBody.CopyToAsync(originalResponseBody);

                    // default to request path
                    logContext.FilterPath = httpContext?.Request?.Path;

                    // override with route template (if applicable)
                    Endpoint endpoint = httpContext.Features.Get<IEndpointFeature>()?.Endpoint;
                    ControllerActionDescriptor controllerActionDescriptor = endpoint?.Metadata?.GetMetadata<ControllerActionDescriptor>();
                    if (controllerActionDescriptor?.AttributeRouteInfo != null)
                    {
                        // use route template
                        logContext.FilterPath = ToPathString(controllerActionDescriptor.AttributeRouteInfo.Template ?? string.Empty);
                    }

                    // find filer
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
                            var responseBody = GetResponseBody(httpContext.Response, bufferSize);
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
            }
            // LogException() will always returns false, to allow the exception to bubble up
            catch (Exception ex) when (LogException(ex, sw, logContext))
            {
            }
            finally
            {
                httpContext.Response.Body = originalResponseBody;
            }
        }

        //private async Task<string> ResponseBodyLogging(HttpContext httpContext, int bufferSize)
        //{
        //    string responseBody;
        //    Stream originalResponseBody = httpContext.Response.Body;
        //    try
        //    {
        //        using (MemoryStream newResponseBody = RecyclableMemoryStreamManager.GetStream())
        //        {
        //            // swap the context response body stream with the memory stream
        //            httpContext.Response.Body = newResponseBody;
        //            await _next(httpContext);
        //            // reset position to 0
        //            newResponseBody.Seek(0, SeekOrigin.Begin);
        //            // Copy the response body to the original body stream
        //            await newResponseBody.CopyToAsync(originalResponseBody);
        //            // Get response body in text format
        //            responseBody = GetResponseBody(httpContext.Response, bufferSize);
        //        }
        //    }
        //    finally
        //    {
        //        httpContext.Response.Body = originalResponseBody;
        //    }
        //    return responseBody;
        //}

        private bool LogException(Exception ex, Stopwatch sw, LogContext logContext)
        {
            sw.Stop();
            logContext.Elapsed = sw.Elapsed;
            LogbookService.LogException(logContext, ex);
            return false; // LogException() will always returns false, to allow the exception to bubble up
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request, int bufferSize)
        {
            using (var requestStream = RecyclableMemoryStreamManager.GetStream())
            {
                request.Body.Seek(0, SeekOrigin.Begin);
                await request.Body.CopyToAsync(requestStream);
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

        private static string ToPathString(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            if (!val.StartsWith("/"))
            {
                return "/" + val;
            }
            return val;
        }
    }
}