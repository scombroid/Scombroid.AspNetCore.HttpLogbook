using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Scombroid.AspNetCore.HttpLogbook;

namespace Microsoft.AspNetCore.Builder
{
    public static class HttpLogbookApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseLogbook(this IApplicationBuilder app)
        {
            return UseLogbook(app, new HttpLogbookMiddlewareOptions());
        }

        public static IApplicationBuilder UseLogbook(this IApplicationBuilder app, HttpLogbookMiddlewareOptions options)
        {
            app.Validate();

            app.UseMiddleware<HttpLogbookMiddleware>(options);

            return app;
        }

        internal static void Validate(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            var logger = loggerFactory.CreateLogger("Logbook.Startup");

            var scopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

            // make sure all http log book components are available
            using (var scope = scopeFactory.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                // filter
                TestService(serviceProvider, typeof(IHttpLogbookFilter), logger, "No filter specified for http log book", true);
                var httpLogbookFilter = serviceProvider.GetService(typeof(IHttpLogbookFilter));
                logger.LogInformation($"Using {httpLogbookFilter.GetType().FullName} as the http log book filter");

                // logger
                TestService(serviceProvider, typeof(IHttpLogbookLogger), logger, "No logger specified for http log book", true);
                var httpLogbookLogger = serviceProvider.GetService(typeof(IHttpLogbookLogger));
                logger.LogInformation($"Using {httpLogbookLogger.GetType().FullName} as the http log book logger");
            }
        }

        private static object TestService(IServiceProvider serviceProvider, Type service, ILogger logger, string message = null, bool doThrow = true)
        {
            var appService = serviceProvider.GetService(service);

            if (appService == null)
            {
                var error = message ?? $"Required service {service.FullName} is not registered in the DI container. Aborting startup";

                logger.LogCritical(error);

                if (doThrow)
                {
                    throw new InvalidOperationException(error);
                }
            }

            return appService;
        }
    }
}
