﻿using System;
using System.Linq;
using IdentityServerWithAspIdAndEF;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace IdentityServerWithAspNetIdentity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "IdentityServerWithAspNetIdentity";

            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .MinimumLevel.Override("Microsoft", LogEventLevel.Verbose)
               .MinimumLevel.Override("System", LogEventLevel.Verbose)
               .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Verbose)
               .Enrich.FromLogContext()
               .WriteTo.File(@"identityserver4_log.txt")
               .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Literate)
               .CreateLogger();

            var seed = args.Contains("/seed");
            //if (seed)
            //{
                Console.WriteLine("Arg \"seed\" found.");
                args = args.Except(new[] { "/seed" }).ToArray();
            //}

            var host = BuildWebHost(args);

            //if (seed)
            //{
                SeedData.EnsureSeedData(host.Services);
            //}

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                })
                .UseStartup<Startup>()
                .Build();
    }
}
