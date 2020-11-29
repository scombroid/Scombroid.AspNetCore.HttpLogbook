﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scombroid.AspNetCore.HttpLogbook;
using Scombroid.AspNetCore.HttpLogbook.DependencyInjection;
using Scombroid.AspNetCore.HttpLogbook.Filters;
using Scombroid.AspNetCore.HttpLogbook.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpLogbookServiceCollectionExtensions
    {
        public static IHttpLogbookBuilder AddLogbookBuilder(this IServiceCollection services)
        {
            return new HttpLogbookBuilder(services);
        }

        public static IHttpLogbookBuilder AddLogbook(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<HttpLogbookConfig>(configuration);
            return services.AddHttpLogbook();
        }

        public static IHttpLogbookBuilder AddHttpLogbook(this IServiceCollection services)
        {
            var builder = services.AddLogbookBuilder();
            builder.Services.TryAddSingleton<IHttpLogbookFilter, HttpLogbookConfigFilter>();
            builder.Services.TryAddSingleton<IHttpLogbookLogger, DefaultHttpLogbookLogger>();
            return new HttpLogbookBuilder(services);
        }

        public static IHttpLogbookBuilder AddLogger<T>(this IHttpLogbookBuilder builder)
           where T : class, IHttpLogbookLogger
        {
            builder.Services.AddSingleton<IHttpLogbookLogger, T>();
            return builder;
        }

        public static IHttpLogbookBuilder AddFilter<T>(this IHttpLogbookBuilder builder)
           where T : class, IHttpLogbookFilter
        {
            builder.Services.AddSingleton<IHttpLogbookFilter, T>();
            return builder;
        }
    }
}
