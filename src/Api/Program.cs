// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "API";

            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .MinimumLevel.Override("Microsoft", LogEventLevel.Verbose)
               .MinimumLevel.Override("System", LogEventLevel.Verbose)
               .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Verbose)
               .Enrich.FromLogContext()
               .WriteTo.File(@"identityserver4_log.txt")
               .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Literate)
               .CreateLogger();

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                })
                .Build();
    }
}