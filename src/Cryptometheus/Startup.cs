//
//          Copyright Seth Hendrick 2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace Cryptometheus
{
    public class Startup
    {
        // ---------------- Constructor ----------------

        public Startup( IConfiguration configuration )
        {
            Configuration = configuration;
        }

        // ---------------- Properties ----------------

        public IConfiguration Configuration { get; }

        // ---------------- Functions ----------------

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        public void ConfigureServices( IServiceCollection services )
        {
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        public void Configure( IApplicationBuilder app, IWebHostEnvironment env )
        {
            app.UseRouting();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapMetrics();
                }
            );
        }
    }
}
