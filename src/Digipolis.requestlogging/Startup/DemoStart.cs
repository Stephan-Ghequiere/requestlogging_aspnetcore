using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Digipolis.Requestlogging.Startup
{
    public class DemoStart
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRequestLogging(
                incoming: options =>
                {
                    options.ExcludedPaths = new[] {"swagger", "hangfire", "favicon"};
                    options.IncludeBody = true;
                    options.ExcludedBodyProperties = new[] {"photo", "userid"};
                },
                outgoing: options =>
                {
                    options.ExcludedPaths = new[] {"status"};
                }
            );
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRequestLogging();
        }
    }
}
