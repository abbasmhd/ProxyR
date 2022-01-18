using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProxyR.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI
{
    public class Startup
    {
        private readonly string _connectionString;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _connectionString = configuration.GetConnectionString("localDb");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            // Adds the configuration for the database functions queries.
            services.AddProxyR(options => options.BindConfiguration(Configuration.GetSection("ProxyR"))
                                                 .UseConnectionString(_connectionString));

            // services.AddRazorPages();

            //services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            //app.UseRouting();

            //app.UseAuthorization();

            //app.UseEndpoints(endpoints => {
            //    endpoints.MapControllers();
            //});

            // Lookout for calls for ProxyR.
            //app.UseWhen(c => c.User.Identity?.IsAuthenticated != true,
            //            authenticated => app.UseProxyR());

            //app.UseProxyR(options => options.BindConfiguration(Configuration.GetSection("ProxyR"))
            //                                     .UseConnectionString(_connectionString));

            app.UseProxyR();
            // start url
            // /users/grid
        }
    }
}
