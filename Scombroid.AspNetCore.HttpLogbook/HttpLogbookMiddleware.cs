using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Scombroid.AspNetCore.HttpLogbook.Filters;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;

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
            if (this.LogbookFilter.IsEnabled())
            {
                await Process(httpContext);
            }
            else
            {
                await _next(httpContext);
            }
        }

        public async Task Process(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));
            var sw = Stopwatch.StartNew();
            string ipAddress = null;

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
                    var path = httpContext?.Request?.Path;

                    // override with route template (if applicable)
                    Endpoint endpoint = httpContext.Features.Get<IEndpointFeature>()?.Endpoint;
                    ControllerActionDescriptor controllerActionDescriptor = endpoint?.Metadata?.GetMetadata<ControllerActionDescriptor>();
                    if (controllerActionDescriptor?.AttributeRouteInfo != null)
                    {
                        // use route template
                        path = ToPathString(controllerActionDescriptor.AttributeRouteInfo.Template ?? string.Empty);
                    }

                    var filter = LogbookFilter.Find(path, httpContext?.Request?.Method);

                    // process request
                    LogLevel requestLogLevel = (filter != null) ? filter.GetRequestLogLevel() : LogLevel.None;
                    switch (requestLogLevel)
                    {
                        case LogLevel.Trace:
                            await RequestTraceLogging(httpContext, path, filter.Request);
                            break;
                        case LogLevel.Information:
                            RequestInfoLogging(httpContext, path, filter.Request);
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
                            ResponseTraceLogging(httpContext, path, filter.Response, sw);
                            break;
                        case LogLevel.Information:
                            ResponseInfoLogging(httpContext, path, filter.Response, sw);
                            break;
                        case LogLevel.None:
                        default:
                            break;
                    }
                }
            }
            // LogException() will always returns false, to allow the exception to bubble up
            catch (Exception ex) when (LogException(ipAddress, httpContext, ex, sw))
            {
            }
            finally
            {
                httpContext.Response.Body = originalResponseBody;
            }
        }

        private void RequestInfoLogging(HttpContext httpContext, PathString? path, HttpLogbookMessageFilter messageFilter)
        {
            LogbookService.LogRequest(new RequestLogContext()
            {
                LogLevel = LogLevel.Information,
                FilterPath = path?.Value,
                MessageFilter = messageFilter,
                HttpRequest = httpContext.Request,
                Body = null
            });
        }

        private async Task RequestTraceLogging(HttpContext httpContext, PathString? path, HttpLogbookMessageFilter messageFilter)
        {
            string requestBody = await GetRequestBodyAsync(httpContext.Request);
            if (messageFilter != null)
            {
                messageFilter.ApplyBodyMask(ref requestBody);
            }

            LogbookService.LogRequest(new RequestLogContext()
            {
                LogLevel = LogLevel.Trace,
                FilterPath = path?.Value,
                MessageFilter = messageFilter,
                HttpRequest = httpContext.Request,
                Body = requestBody
            });
        }

        private void ResponseInfoLogging(HttpContext httpContext, PathString? path, HttpLogbookMessageFilter messageFilter, Stopwatch sw)
        {
            sw.Stop();

            LogbookService.LogResponse(new ResponseLogContext()
            {
                LogLevel = LogLevel.Information,
                FilterPath = path?.Value,
                MessageFilter = messageFilter,
                HttpResponse = httpContext.Response,
                Elapsed = sw.Elapsed,
                Body = null
            });
        }

        private void ResponseTraceLogging(HttpContext httpContext, PathString? path, HttpLogbookMessageFilter messageFilter, Stopwatch sw)
        {
            // Get response body in text format
            string responseBody = GetResponseBody(httpContext.Response, BufferSize);
            if (messageFilter != null)
            {
                messageFilter.ApplyBodyMask(ref responseBody);
            }
            sw.Stop();

            LogbookService.LogResponse(new ResponseLogContext()
            {
                LogLevel = LogLevel.Trace,
                FilterPath = path?.Value,
                MessageFilter = messageFilter,
                HttpResponse = httpContext.Response,
                Elapsed = sw.Elapsed,
                Body = responseBody
            });
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
                var pos = request.Body.Position; // get current position
                request.Body.Seek(0, SeekOrigin.Begin);
                await request.Body.CopyToAsync(requestStream);
                request.Body.Position = pos; // reset back to original position
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

        private static PathString? ToPathString(string val)
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