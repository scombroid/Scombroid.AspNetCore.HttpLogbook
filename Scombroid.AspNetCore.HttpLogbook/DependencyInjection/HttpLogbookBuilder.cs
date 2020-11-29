using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scombroid.AspNetCore.HttpLogbook.DependencyInjection
{
    /// <summary>
    /// Logbook helper class for DI configuration
    /// </summary>
    public class HttpLogbookBuilder : IHttpLogbookBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpLogbookBuilder"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        public HttpLogbookBuilder(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            Services = services;
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>
        /// The services.
        /// </value>
        public IServiceCollection Services { get; }
    }
}
