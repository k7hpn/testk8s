using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

namespace TestK8s
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration configuration)
        {
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            if (!string.IsNullOrEmpty(_config["ReverseProxy"]))
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All,
                    RequireHeaderSymmetry = false,
                    ForwardLimit = null,
                    KnownProxies = {
                        System.Net.IPAddress.Parse(_config["ReverseProxy"])
                    }
                });
            }

            app.Use(async (context, next) =>
            {
                using (Serilog.Context
                    .LogContext
                    .PushProperty("TraceIdentifier", context.TraceIdentifier))
                {
                    using (Serilog.Context
                        .LogContext
                        .PushProperty("RemoteAddress", context.Connection.RemoteIpAddress))
                    {
                        await next.Invoke();
                    }
                }
            });

            app.UseStaticFiles();

            app.Use(async (context, next) =>
            {
                if (!context.Session.Keys.Contains("CreatedAt"))
                {
                    context.Session.Set("CreatedAt", BitConverter.GetBytes(DateTime.Now.Ticks));
                    context.Session.Set("CreatedLocalIpAddress", context.Connection.LocalIpAddress.GetAddressBytes());
                    context.Session.Set("CreatedLocalPort", BitConverter.GetBytes(context.Connection.LocalPort));
                    context.Session.Set("CreatedRemoteIpAddress", context.Connection.RemoteIpAddress.GetAddressBytes());
                    context.Session.Set("CreatedRemoteIpPort", BitConverter.GetBytes(context.Connection.RemotePort));
                }
                await next.Invoke();
            });

            app.UseRouting();

            //app.UseAuthorization();
            
            app.UseSession();

            app.UseEndpoints(_ =>
            {
                _.MapControllers();
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (!string.IsNullOrEmpty(_config["RedisConfiguration"]))
            {
                var instanceName = _config["RedisInstance"] ?? "TestK8s";

                services.AddStackExchangeRedisCache(_ =>
                {
                    _.Configuration = _config["RedisConfiguration"];
                    _.InstanceName = instanceName;
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            services.AddSession(_ =>
            {
                //_.IdleTimeout = TimeSpan.FromMinutes(15);
                _.Cookie.HttpOnly = true;
                _.Cookie.IsEssential = true;
            });

            services.AddControllersWithViews();
        }
    }
}