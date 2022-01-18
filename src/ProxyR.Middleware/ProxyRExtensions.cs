using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProxyR.Abstractions;
using ProxyR.Core.Extensions;

namespace ProxyR.Middleware
{
    public static class ProxyRExtensions
    {

        /// <summary>
        ///     Configures ProxyR.
        /// </summary>
        public static IServiceCollection AddProxyR(this IServiceCollection services) => services;

        /// <summary>
        ///     Configures ProxyR.
        /// </summary>
        public static IServiceCollection AddProxyR(this IServiceCollection services, IConfigurationSection configurationSection)
        {
            services.Configure<ProxyROptions>(configurationSection);

            return services;
        }

        /// <summary>
        ///     Configures ProxyR, with an options builder.
        /// </summary>
        public static IServiceCollection AddProxyR(this IServiceCollection services, Action<ProxyROptionsBuilder> builderFunc)
        {
            var optionsBuilder = new ProxyROptionsBuilder();
            builderFunc?.Invoke(optionsBuilder);
            services.Configure<ProxyROptions>(target => optionsBuilder.Options.Copy(target));
            services.Configure<ProxyRRuntimeOptions>(target => optionsBuilder.RuntimeOptions.Copy(target));

            return services;
        }

        /// <summary>
        ///     Adds ProxyR calls to the HTTP pipeline.
        /// </summary>
        public static IApplicationBuilder UseProxyR(this IApplicationBuilder builder) => builder.UseMiddleware<ProxyRMiddleware>();

        /// <summary>
        ///     Adds ProxyR calls to the HTTP pipeline.
        /// </summary>
        public static IApplicationBuilder UseProxyR(this IApplicationBuilder builder, Action<ProxyROptionsBuilder> builderFunc)
        {
            var optionsBuilder = new ProxyROptionsBuilder();
            builderFunc?.Invoke(optionsBuilder);
            var optionsWrapper = new OptionsWrapper<ProxyROptions>(optionsBuilder.Options);
            var runtimeOptionsWrapper = new OptionsWrapper<ProxyRRuntimeOptions>(optionsBuilder.RuntimeOptions);

            return builder.UseMiddleware<ProxyRMiddleware>(optionsWrapper, runtimeOptionsWrapper);
        }

    }
}
