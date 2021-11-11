using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Reflection;

namespace TestK8s
{
    public static class Program
    {
        private const string Product = "TestK8s";

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(_ => _.UseStartup<Startup>())
                .UseSerilog();

        public static int Main(string[] args)
        {
            using var webHost = CreateHostBuilder(args).Build();
            var config = (IConfiguration)webHost.Services.GetService(typeof(IConfiguration));

            var instance = string.IsNullOrEmpty(config["Instance"])
                ? "n/a"
                : config["Instance"];

            var version = GetVersion();

            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.WithProperty("Application", Product)
                .Enrich.WithProperty("Version", version)
                .Enrich.FromLogContext()
                .WriteTo.Console();

            if (!string.IsNullOrEmpty(instance))
            {
                loggerConfig.Enrich.WithProperty("Instance", instance);
            }

            string seqEndpoint = config["SeqEndpoint"];
            if (!string.IsNullOrEmpty(seqEndpoint))
            {
                loggerConfig
                    .WriteTo.Logger(_ => _
                        .WriteTo.Seq(seqEndpoint,
                            apiKey: config["SeqApiKey"]));
            }

            Log.Logger = loggerConfig.CreateLogger();

            Log.Information("{Product} v{Version} instance {Instance} starting up",
                Product,
                version,
                instance);
            try
            {
                if (string.IsNullOrEmpty(config["RedisConfiguration"]))
                {
                    Log.Information("Distributed cache: in memory");
                }
                else
                {
                    Log.Information("Distributed cache: Redis configuration {RedisConfiguration} instance {RedisInstance}",
                        config["RedisConfiguration"],
                        config["RedisInstance"] ?? "TestK8s");
                }

                webHost.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "{Product} instance {Instance} v{Version} exited unexpectedly: {Message}",
                    Product,
                    instance,
                    version,
                    ex.Message);
                return 1;
            }
            finally
            {
                Log.Information("{Product} instance {Instance} v{Version} shutting down",
                   Product,
                   instance,
                   version);
                Log.CloseAndFlush();
            }
        }

        private static string GetVersion()
        {
            var fileVersion = Assembly
                 .GetEntryAssembly()
                 .GetCustomAttribute<AssemblyFileVersionAttribute>()?
                 .Version;

            return !string.IsNullOrEmpty(fileVersion)
                ? fileVersion
                : Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;
        }
    }
}