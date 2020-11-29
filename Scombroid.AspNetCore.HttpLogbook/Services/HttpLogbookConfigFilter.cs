using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Scombroid.AspNetCore.HttpLogbook.Filters;

namespace Scombroid.AspNetCore.HttpLogbook
{
    internal class HttpLogbookConfigFilter : IHttpLogbookFilter
    {
        private readonly ILogger<HttpLogbookConfigFilter> Logger;
        private HttpLogbookConfig config;
        public HttpLogbookConfigFilter(ILogger<HttpLogbookConfigFilter> logger, IOptionsMonitor<HttpLogbookConfig> configOption)
        {
            Logger = logger;
            ReloadConfig(configOption.CurrentValue);
            configOption.OnChange((errors) => ReloadConfig(errors));
        }

        private void ReloadConfig(HttpLogbookConfig newConfig)
        {
            try
            {
                Logger.LogInformation($"Refreshing http log book config");
                config = newConfig ?? new HttpLogbookConfig();
                Logger.LogInformation($"Found {config?.Paths?.Count} paths in config");
            }
            catch (Exception ex)
            {
                Logger.LogCritical($"Exception occured while refreshing http log book config. Error: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
        }

        public HttpLogbookMethodFilter Find(PathString? path, string method)
        {
            var pathRule = FindPathFilter(path, config.Paths);
            if (pathRule != null)
            {
                return FindMethodFilter(method, pathRule.Methods);
            }
            return null;
        }

        private HttpLogbookPathFilter FindPathFilter(PathString? path, Dictionary<string, HttpLogbookPathFilter> pathFilters)
        {
            HttpLogbookPathFilter pathRule;
            if (pathFilters != null)
            {
                if (path.HasValue)
                {
                    string key = path.Value.Value;
                    if (pathFilters.TryGetValue(key, out pathRule))
                    {
                        return pathRule;
                    }
                }

                // default
                if (pathFilters.TryGetValue(Constants.Paths.All, out pathRule))
                {
                    return pathRule;
                }
            }
            return null;
        }

        private HttpLogbookMethodFilter FindMethodFilter(string method, Dictionary<string, HttpLogbookMethodFilter> methodFilters)
        {
            HttpLogbookMethodFilter methodRule;
            if (methodFilters != null)
            {
                if (methodFilters.TryGetValue(method, out methodRule))
                {
                    return methodRule;
                }

                // default
                if (methodFilters.TryGetValue(Constants.Methods.All, out methodRule))
                {
                    return methodRule;
                }
            }
            return null;
        }
    }
}

