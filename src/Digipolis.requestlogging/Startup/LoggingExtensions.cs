using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Digipolis.Requestlogging
{
    /// <summary>
    /// Extension methods for configuring request logging.
    /// </summary>
    public static class RequestLoggingExtensions
    {
        /// <summary>
        /// Registers the ApplicationLogger for DI.
        /// Registers options setupaction for serilog
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="incoming">The incoming.</param>
        /// <param name="outgoing">The outgoing.</param>
        /// <returns></returns>
        public static IServiceCollection AddRequestLogging(this IServiceCollection services, Action<IncomingRequestLoggingOptions> incoming = null, Action<OutgoingRequestLoggingOptions> outgoing = null)
        {
            services.AddScoped<IExternalServiceTimer, ExternalServiceTimer>();

            if (!(incoming is null))
                services.Configure<IncomingRequestLoggingOptions>(incoming);
            if (!(outgoing is null))
                services.Configure<OutgoingRequestLoggingOptions>(outgoing);

            return services;
        }

        /// <summary>
        /// Adds the loggermiddleware to the request pipeline.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<LoggerMiddleware>();
        }
    }
}
